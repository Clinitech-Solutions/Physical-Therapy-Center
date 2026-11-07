using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using PTCenter.Infrastructure.Services;
using Xunit;

namespace PTCenter.Tests;

public class DocumentUploadPolicyTests
{
    private static MultipartFormDataContent Form(int patient, byte[] bytes, string name, string mime = "application/octet-stream", string? key=null)
    {
        var data = new MultipartFormDataContent(); data.Add(new StringContent(patient.ToString()), "PatientId");
        if(key!=null)data.Add(new StringContent(key),"IdempotencyKey");
        var file = new ByteArrayContent(bytes); file.Headers.ContentType = new MediaTypeHeaderValue(mime); data.Add(file, "Files", name); return data;
    }

    [Fact]
    public async Task Upload_replay_returns_one_document_and_changed_content_cannot_reuse_the_original_operation()
    {
        using var host=new BookingTestHost();await ReceptionAccessFixture.SeedAsync(host);
        using var client=host.CreateClient(new(){BaseAddress=new("https://localhost")});await ReceptionAccessFixture.LoginAsync(client);
        var env=host.Services.GetRequiredService<IWebHostEnvironment>();env.WebRootPath=Path.Combine(Path.GetTempPath(),"ptcenter-upload-retry-"+Guid.NewGuid().ToString("N"));
        var key=Guid.NewGuid().ToString();var pdf=Encoding.ASCII.GetBytes("%PDF-1.7\nUAT260 replay\n%%EOF");
        using var first=Form(1,pdf,"retry.pdf",key:key);var saved=await client.PostAsync("/api/InsuranceDocuments/Add",first);Assert.Equal(HttpStatusCode.OK,saved.StatusCode);
        var record=(await saved.Content.ReadFromJsonAsync<JsonElement>())[0];
        using var replay=Form(1,pdf,"retry.pdf",key:key);var response=await client.PostAsync("/api/InsuranceDocuments/Add",replay);Assert.Equal(HttpStatusCode.OK,response.StatusCode);
        var again=(await response.Content.ReadFromJsonAsync<JsonElement>())[0];Assert.Equal(record.GetProperty("id").GetInt32(),again.GetProperty("id").GetInt32());Assert.Equal(record.GetProperty("filePath").GetString(),again.GetProperty("filePath").GetString());
        using var changed=Form(1,Encoding.ASCII.GetBytes("%PDF-1.7\nChanged content\n%%EOF"),"retry.pdf",key:key);Assert.Equal(HttpStatusCode.Conflict,(await client.PostAsync("/api/InsuranceDocuments/Add",changed)).StatusCode);
        Assert.Single((await client.GetFromJsonAsync<JsonElement>("/api/InsuranceDocuments?patientId=1")).EnumerateArray());
        Assert.Equal(pdf,await client.GetByteArrayAsync($"/api/InsuranceDocuments/{record.GetProperty("id").GetInt32()}/file"));
    }

    [Fact]
    public async Task Upload_checks_signature_size_and_authorization_and_preserves_existing_document_on_replacement_failure()
    {
        using var host = new BookingTestHost(); await ReceptionAccessFixture.SeedAsync(host);
        using var client = host.CreateClient(new() { BaseAddress = new("https://localhost") });
        await ReceptionAccessFixture.LoginAsync(client);
        var environment = host.Services.GetRequiredService<IWebHostEnvironment>();
        var directory = Path.Combine(Path.GetTempPath(), "ptcenter-upload-policy-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory); environment.WebRootPath = directory;
        var pdf = Encoding.ASCII.GetBytes("%PDF-1.7\nSynthetic UAT document\n%%EOF");
        using var valid = Form(1, pdf, "consent.PDF", "text/plain");
        var uploaded = await client.PostAsync("/api/InsuranceDocuments/Add", valid); Assert.Equal(HttpStatusCode.OK, uploaded.StatusCode);
        var record = (await uploaded.Content.ReadFromJsonAsync<JsonElement>())[0]; var id = record.GetProperty("id").GetInt32();
        Assert.Equal("application/pdf", record.GetProperty("contentType").GetString());
        Assert.Equal(pdf, await client.GetByteArrayAsync($"/api/InsuranceDocuments/{id}/file"));
        foreach (var (bytes, name) in new[] { (Array.Empty<byte>(), "empty.pdf"), (pdf, "unsupported.exe"), (Encoding.ASCII.GetBytes("not a PDF"), "misleading.pdf"), (new byte[10_485_761], "oversized.png") })
        {
            using var invalid = Form(1, bytes, name, "application/pdf");
            Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsync("/api/InsuranceDocuments/Add", invalid)).StatusCode);
        }
        var exact = new byte[InsuranceDocumentService.MaximumFileBytes]; pdf.CopyTo(exact, 0);
        using var boundary = Form(1, exact, "exactly-10-MiB.pdf"); Assert.Equal(HttpStatusCode.OK, (await client.PostAsync("/api/InsuranceDocuments/Add", boundary)).StatusCode);
        using var replacement = new MultipartFormDataContent(); replacement.Add(new ByteArrayContent(Encoding.ASCII.GetBytes("wrong signature")), "File", "replacement.pdf");
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PutAsync($"/api/InsuranceDocuments/{id}", replacement)).StatusCode);
        Assert.Equal(pdf, await client.GetByteArrayAsync($"/api/InsuranceDocuments/{id}/file"));
        await ReceptionAccessFixture.LoginAsync(client, "Doctor");
        using var forbidden = Form(1, pdf, "forbidden.pdf"); Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsync("/api/InsuranceDocuments/Add", forbidden)).StatusCode);
        client.DefaultRequestHeaders.Authorization = null;
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync($"/api/InsuranceDocuments/{id}/file")).StatusCode);
        // Deliberately retain this isolated temporary folder for inspection; no existing document is removed.
    }
}

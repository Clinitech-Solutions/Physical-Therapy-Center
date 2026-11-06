using System.Globalization;
using System.Text;

namespace PTCenter.Infrastructure.Services;

/// <summary>Shared UTF-8 CSV quoting and spreadsheet-formula neutralization.</summary>
public sealed class FinancialCsv
{
    private readonly StringBuilder text=new("\uFEFF");
    public void Line(params object?[] values) => text.AppendLine(string.Join(",",values.Select(Escape)));
    public byte[] Bytes() => Encoding.UTF8.GetBytes(text.ToString());
    public static string Escape(object? value)
    {
        var text=Convert.ToString(value,CultureInfo.InvariantCulture) ?? "";
        if(text.TrimStart().FirstOrDefault() is '=' or '+' or '-' or '@' or '\t' or '\r' or '\n')text="'"+text;
        return "\""+text.Replace("\"","\"\"")+"\"";
    }
}

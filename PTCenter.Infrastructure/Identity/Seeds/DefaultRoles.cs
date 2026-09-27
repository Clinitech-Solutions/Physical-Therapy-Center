namespace PTCenter.Infrastructure.Identity.Seeds;

public static class DefaultRoles
{
    public partial class Admin
    {
        public const string Name = nameof(Admin);
        public const string Id = "0191d311-2918-7f76-bd8a-0bded8535075";
        public const string ConcurrencyStamp = "0191d311-2918-7f76-bd8a-0be08dd83078";
    }


    public partial class Owner
    {
        public const string Name = nameof(Owner);
        public const string Id = "0191d311-2918-7f76-bd8a-0bdf740adff8";
        public const string ConcurrencyStamp = "0191d311-2918-7f76-bd8a-0be100f11384";
    }

    public partial class Doctor
    {
        public const string Name = nameof(Doctor);
        public const string Id = "0191d311-2918-7f76-bd8a-0bdf740adff9";
        public const string ConcurrencyStamp = "0191d311-2918-7f76-bd8a-0be100f11385";
    }
    public partial class Patient
    {
        public const string Name = nameof(Patient);
        public const string Id = "feb07bce-2808-40bb-8561-9b95ed1d2816";
        public const string ConcurrencyStamp = "644f048f-1dcf-4b6c-b21c-55a24018046e";
    }
    public partial class Senior
    {
        public const string Name = nameof(Senior);
        public const string Id = "0191d311-2918-7f76-bd8a-0bdf740adffa";
        public const string ConcurrencyStamp = "0191d311-2918-7f76-bd8a-0be100f11386";
    }
    public partial class Receptionist
    {
        public const string Name = nameof(Receptionist);
        public const string Id = "0191d311-2918-7f76-bd8a-0bdf740adffb";
        public const string ConcurrencyStamp = "0191d311-2918-7f76-bd8a-0be100f11387";
    }      
    public partial class Ceo
    {
        public const string Name = nameof(Ceo);
        public const string Id = "0191d311-2918-7f76-bd8a-0bdf740adffc";
        public const string ConcurrencyStamp = "0191d311-2918-7f76-bd8a-0be100f11388";
    }



}

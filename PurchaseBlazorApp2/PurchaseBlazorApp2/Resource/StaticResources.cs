namespace PurchaseBlazorApp2.Resource
{
    static class StaticResources
    {
        // public const string ConnectionId = "localhost";
        //public const string ConnectionId = "einvoice.cdnonchautom.ap-southeast-1.rds.amazonaws.com";
        //
        private const string LocalhostConnectionId = "localhost";
        private const string AwsConnectionId = "einvoice.cdnonchautom.ap-southeast-1.rds.amazonaws.com";
        public static string BaseAddress { get; set; } = "einvoice.cdnonchautom.ap-southeast-1.rds.amazonaws.com";
        public static string ConnectionId()
        {
            var baseAddress = BaseAddress ?? "localhost";

            if (baseAddress.Contains("localhost"))
                return LocalhostConnectionId;

            if (baseAddress.StartsWith("https://purchase.genesis-e-invoice.com/"))
                return AwsConnectionId;

            return LocalhostConnectionId;
        }

    }
}

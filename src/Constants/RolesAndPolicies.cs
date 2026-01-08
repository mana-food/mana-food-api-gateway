namespace ApiGateway.Constants;

public static class Roles
{
    public const string ADMIN = "ADMIN";
    public const string CUSTOMER = "CUSTOMER";
    public const string KITCHEN = "KITCHEN";
    public const string OPERATOR = "OPERATOR";
    public const string MANAGER = "MANAGER";
}

public static class Policies
{
    public const string ADMIN_ONLY = "AdminOnly";
    public const string ADMIN_OR_MANAGER = "AdminOrManager";
    public const string KITCHEN_STAFF = "KitchenStaff";
    public const string OPERATORS = "Operators";
    public const string MANAGEMENT = "Management";
    public const string AUTHENTICATED_USER = "AuthenticatedUser";
    public const string ORDER_MANAGEMENT = "OrderManagement";
    public const string DATA_QUERY = "DataQuery";

}
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProcurementPlanner.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPerformanceIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_SupplierPerformance_LastUpdated_SupplierId",
                table: "SupplierPerformanceMetrics",
                columns: new[] { "LastUpdated", "SupplierId" });

            migrationBuilder.CreateIndex(
                name: "IX_SupplierPerformance_OnTime_Quality",
                table: "SupplierPerformanceMetrics",
                columns: new[] { "OnTimeDeliveryRate", "QualityScore" });

            migrationBuilder.CreateIndex(
                name: "IX_SupplierCapabilities_ProductType_Active_Capacity",
                table: "SupplierCapabilities",
                columns: new[] { "ProductType", "IsActive", "MaxMonthlyCapacity" });

            migrationBuilder.CreateIndex(
                name: "IX_SupplierCapabilities_SupplierId_Active",
                table: "SupplierCapabilities",
                columns: new[] { "SupplierId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_CreatedBy_CreatedAt",
                table: "PurchaseOrders",
                columns: new[] { "CreatedBy", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_CustomerOrderId_Status",
                table: "PurchaseOrders",
                columns: new[] { "CustomerOrderId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_DeliveryDate_Status",
                table: "PurchaseOrders",
                columns: new[] { "RequiredDeliveryDate", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_SupplierId_Status",
                table: "PurchaseOrders",
                columns: new[] { "SupplierId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_OrderItems_OrderId_ProductCode",
                table: "OrderItems",
                columns: new[] { "OrderId", "ProductCode" });

            migrationBuilder.CreateIndex(
                name: "IX_OrderItems_OrderId_UnitPrice",
                table: "OrderItems",
                columns: new[] { "OrderId", "UnitPrice" });

            migrationBuilder.CreateIndex(
                name: "IX_NotificationLogs_Recipient_Type_Created",
                table: "NotificationLogs",
                columns: new[] { "Recipient", "Type", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_NotificationLogs_Status_Priority_Created",
                table: "NotificationLogs",
                columns: new[] { "Status", "Priority", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_CustomerOrders_CreatedAt_Status",
                table: "CustomerOrders",
                columns: new[] { "CreatedAt", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_CustomerOrders_CustomerId_Status",
                table: "CustomerOrders",
                columns: new[] { "CustomerId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_CustomerOrders_Dashboard",
                table: "CustomerOrders",
                columns: new[] { "ProductType", "RequestedDeliveryDate", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_CustomerOrders_DeliveryDate_Status",
                table: "CustomerOrders",
                columns: new[] { "RequestedDeliveryDate", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_CustomerOrders_OverdueQuery",
                table: "CustomerOrders",
                columns: new[] { "RequestedDeliveryDate", "Status", "ProductType" });

            migrationBuilder.CreateIndex(
                name: "IX_CustomerOrders_ProductType_Status",
                table: "CustomerOrders",
                columns: new[] { "ProductType", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_CustomerOrders_Status_DeliveryDate",
                table: "CustomerOrders",
                columns: new[] { "Status", "RequestedDeliveryDate" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_Entity_Timestamp",
                table: "AuditLogs",
                columns: new[] { "EntityType", "EntityId", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_Timestamp_Action",
                table: "AuditLogs",
                columns: new[] { "Timestamp", "Action" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_User_Action_Timestamp",
                table: "AuditLogs",
                columns: new[] { "UserId", "Action", "Timestamp" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SupplierPerformance_LastUpdated_SupplierId",
                table: "SupplierPerformanceMetrics");

            migrationBuilder.DropIndex(
                name: "IX_SupplierPerformance_OnTime_Quality",
                table: "SupplierPerformanceMetrics");

            migrationBuilder.DropIndex(
                name: "IX_SupplierCapabilities_ProductType_Active_Capacity",
                table: "SupplierCapabilities");

            migrationBuilder.DropIndex(
                name: "IX_SupplierCapabilities_SupplierId_Active",
                table: "SupplierCapabilities");

            migrationBuilder.DropIndex(
                name: "IX_PurchaseOrders_CreatedBy_CreatedAt",
                table: "PurchaseOrders");

            migrationBuilder.DropIndex(
                name: "IX_PurchaseOrders_CustomerOrderId_Status",
                table: "PurchaseOrders");

            migrationBuilder.DropIndex(
                name: "IX_PurchaseOrders_DeliveryDate_Status",
                table: "PurchaseOrders");

            migrationBuilder.DropIndex(
                name: "IX_PurchaseOrders_SupplierId_Status",
                table: "PurchaseOrders");

            migrationBuilder.DropIndex(
                name: "IX_OrderItems_OrderId_ProductCode",
                table: "OrderItems");

            migrationBuilder.DropIndex(
                name: "IX_OrderItems_OrderId_UnitPrice",
                table: "OrderItems");

            migrationBuilder.DropIndex(
                name: "IX_NotificationLogs_Recipient_Type_Created",
                table: "NotificationLogs");

            migrationBuilder.DropIndex(
                name: "IX_NotificationLogs_Status_Priority_Created",
                table: "NotificationLogs");

            migrationBuilder.DropIndex(
                name: "IX_CustomerOrders_CreatedAt_Status",
                table: "CustomerOrders");

            migrationBuilder.DropIndex(
                name: "IX_CustomerOrders_CustomerId_Status",
                table: "CustomerOrders");

            migrationBuilder.DropIndex(
                name: "IX_CustomerOrders_Dashboard",
                table: "CustomerOrders");

            migrationBuilder.DropIndex(
                name: "IX_CustomerOrders_DeliveryDate_Status",
                table: "CustomerOrders");

            migrationBuilder.DropIndex(
                name: "IX_CustomerOrders_OverdueQuery",
                table: "CustomerOrders");

            migrationBuilder.DropIndex(
                name: "IX_CustomerOrders_ProductType_Status",
                table: "CustomerOrders");

            migrationBuilder.DropIndex(
                name: "IX_CustomerOrders_Status_DeliveryDate",
                table: "CustomerOrders");

            migrationBuilder.DropIndex(
                name: "IX_AuditLogs_Entity_Timestamp",
                table: "AuditLogs");

            migrationBuilder.DropIndex(
                name: "IX_AuditLogs_Timestamp_Action",
                table: "AuditLogs");

            migrationBuilder.DropIndex(
                name: "IX_AuditLogs_User_Action_Timestamp",
                table: "AuditLogs");
        }
    }
}

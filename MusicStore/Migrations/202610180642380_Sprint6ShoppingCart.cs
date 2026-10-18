namespace MusicStore.Migrations
{
    using System;
    using System.Data.Entity.Migrations;

    public partial class Sprint6ShoppingCart : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.Carts",
                c => new
                    {
                        RecordId = c.Int(nullable: false, identity: true),
                        CartId = c.String(nullable: false, maxLength: 64),
                        AlbumId = c.Int(nullable: false),
                        Count = c.Int(nullable: false),
                        DateCreated = c.DateTime(nullable: false),
                        RowVersion = c.Binary(nullable: false, fixedLength: true, timestamp: true, storeType: "rowversion"),
                    })
                .PrimaryKey(t => t.RecordId)
                .ForeignKey("dbo.Albums", t => t.AlbumId)
                .Index(t => new { t.CartId, t.AlbumId }, unique: true, name: "UX_Cart_Album");

            CreateTable(
                "dbo.Orders",
                c => new
                    {
                        OrderId = c.Int(nullable: false, identity: true),
                        UserId = c.String(nullable: false, maxLength: 128),
                        FirstName = c.String(nullable: false, maxLength: 80),
                        LastName = c.String(nullable: false, maxLength: 80),
                        Address = c.String(nullable: false, maxLength: 200),
                        City = c.String(nullable: false, maxLength: 80),
                        State = c.String(maxLength: 80),
                        PostalCode = c.String(nullable: false, maxLength: 20),
                        Country = c.String(nullable: false, maxLength: 80),
                        Phone = c.String(nullable: false, maxLength: 30),
                        Email = c.String(nullable: false, maxLength: 254),
                        Total = c.Decimal(nullable: false, precision: 18, scale: 2),
                        OrderDate = c.DateTime(nullable: false),
                    })
                .PrimaryKey(t => t.OrderId);

            CreateTable(
                "dbo.OrderDetails",
                c => new
                    {
                        OrderDetailId = c.Int(nullable: false, identity: true),
                        OrderId = c.Int(nullable: false),
                        AlbumId = c.Int(nullable: false),
                        Quantity = c.Int(nullable: false),
                        UnitPrice = c.Decimal(nullable: false, precision: 18, scale: 2),
                    })
                .PrimaryKey(t => t.OrderDetailId)
                .ForeignKey("dbo.Albums", t => t.AlbumId)
                .ForeignKey("dbo.Orders", t => t.OrderId)
                .Index(t => t.OrderId)
                .Index(t => t.AlbumId);



            Sql("ALTER TABLE dbo.Carts ADD CONSTRAINT CK_Cart_Count CHECK ([Count] BETWEEN 1 AND 99);");
            Sql("ALTER TABLE dbo.Carts ADD CONSTRAINT CK_Cart_Identity CHECK (DATALENGTH(CartId) = 128);");
            Sql("ALTER TABLE dbo.OrderDetails ADD CONSTRAINT CK_OrderDetail_Quantity CHECK (Quantity BETWEEN 1 AND 99);");
            Sql("ALTER TABLE dbo.OrderDetails ADD CONSTRAINT CK_OrderDetail_Price CHECK (UnitPrice > 0);");
            Sql("ALTER TABLE dbo.Orders ADD CONSTRAINT CK_Order_Total CHECK (Total >= 0);");

        }

        public override void Down()
        {
            DropForeignKey("dbo.OrderDetails", "OrderId", "dbo.Orders");
            DropForeignKey("dbo.OrderDetails", "AlbumId", "dbo.Albums");
            DropForeignKey("dbo.Carts", "AlbumId", "dbo.Albums");
            DropIndex("dbo.OrderDetails", new[] { "AlbumId" });
            DropIndex("dbo.OrderDetails", new[] { "OrderId" });
            DropIndex("dbo.Carts", "UX_Cart_Album");
            DropTable("dbo.Orders");
            DropTable("dbo.OrderDetails");
            DropTable("dbo.Carts");
        }
    }
}

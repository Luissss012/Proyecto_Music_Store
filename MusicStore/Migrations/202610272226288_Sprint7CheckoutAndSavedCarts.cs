namespace MusicStore.Migrations
{
    using System;
    using System.Data.Entity.Migrations;

    public partial class Sprint7CheckoutAndSavedCarts : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.UserCarts",
                c => new
                    {
                        UserId = c.String(nullable: false, maxLength: 128),
                        CartId = c.String(nullable: false, maxLength: 64),
                    })
                .PrimaryKey(t => t.UserId)
                .Index(t => t.CartId, unique: true, name: "UX_UserCart_CartId");

            AddColumn("dbo.OrderDetails", "AlbumTitle", c => c.String(maxLength: 160));
            AddColumn("dbo.OrderDetails", "ArtistName", c => c.String(maxLength: 160));
            AddColumn("dbo.Orders", "CheckoutToken", c => c.String(maxLength: 64));
            // Preserve any pre-existing orders instead of assigning the same empty token to all of them.
            Sql(@"UPDATE dbo.Orders SET CheckoutToken=LOWER(CONVERT(varchar(64),HASHBYTES('SHA2_256',CONCAT('legacy-order:',OrderId,':',UserId)),2));
                UPDATE d SET AlbumTitle=a.Title,ArtistName=r.Name FROM dbo.OrderDetails d
                JOIN dbo.Albums a ON d.AlbumId=a.AlbumId JOIN dbo.Artists r ON a.ArtistId=r.ArtistId;");
            AlterColumn("dbo.OrderDetails", "AlbumTitle", c => c.String(nullable: false, maxLength: 160));
            AlterColumn("dbo.OrderDetails", "ArtistName", c => c.String(nullable: false, maxLength: 160));
            AlterColumn("dbo.Orders", "CheckoutToken", c => c.String(nullable: false, maxLength: 64));
            CreateIndex("dbo.Orders", "UserId", name: "IX_Order_UserId");
            CreateIndex("dbo.Orders", "CheckoutToken", unique: true, name: "UX_Order_CheckoutToken");
            Sql("ALTER TABLE dbo.Orders ADD CONSTRAINT CK_Order_CheckoutToken CHECK (DATALENGTH(CheckoutToken)=128);");
            Sql("ALTER TABLE dbo.UserCarts ADD CONSTRAINT CK_UserCart_Identity CHECK (DATALENGTH(CartId)=128);");
        }

        public override void Down()
        {
            Sql("ALTER TABLE dbo.Orders DROP CONSTRAINT CK_Order_CheckoutToken;");
            DropIndex("dbo.UserCarts", "UX_UserCart_CartId");
            DropIndex("dbo.Orders", "UX_Order_CheckoutToken");
            DropIndex("dbo.Orders", "IX_Order_UserId");
            DropColumn("dbo.Orders", "CheckoutToken");
            DropColumn("dbo.OrderDetails", "ArtistName");
            DropColumn("dbo.OrderDetails", "AlbumTitle");
            DropTable("dbo.UserCarts");
        }
    }
}

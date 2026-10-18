namespace MusicStore.Migrations
{
    using System;
    using System.Data.Entity.Migrations;

    public partial class AdminCatalogIntegrity : DbMigration
    {
        public override void Up()
        {
            // Abort rather than truncate, replace, or merge existing catalog data.
            Sql(@"IF EXISTS (SELECT 1 FROM dbo.Artists WHERE Name IS NULL OR LEN(LTRIM(RTRIM(Name)))=0 OR DATALENGTH(Name)>320)
                OR EXISTS (SELECT 1 FROM dbo.Genres WHERE Name IS NULL OR LEN(LTRIM(RTRIM(Name)))=0 OR DATALENGTH(Name)>320 OR DATALENGTH(Description)>2000)
                OR EXISTS (SELECT Name FROM dbo.Artists GROUP BY Name HAVING COUNT(*)>1)
                OR EXISTS (SELECT Name FROM dbo.Genres GROUP BY Name HAVING COUNT(*)>1)
                THROW 51000, 'Catalog data must be reviewed before applying administrative constraints.', 1;");
            DropForeignKey("dbo.Albums", "ArtistId", "dbo.Artists");
            DropForeignKey("dbo.Albums", "GenreId", "dbo.Genres");
            AddColumn("dbo.Albums", "RowVersion", c => c.Binary(nullable: false, fixedLength: true, timestamp: true, storeType: "rowversion"));
            AddColumn("dbo.Artists", "RowVersion", c => c.Binary(nullable: false, fixedLength: true, timestamp: true, storeType: "rowversion"));
            AddColumn("dbo.Genres", "RowVersion", c => c.Binary(nullable: false, fixedLength: true, timestamp: true, storeType: "rowversion"));
            AlterColumn("dbo.Artists", "Name", c => c.String(nullable: false, maxLength: 160));
            AlterColumn("dbo.Genres", "Name", c => c.String(nullable: false, maxLength: 160));
            AlterColumn("dbo.Genres", "Description", c => c.String(maxLength: 1000));
            CreateIndex("dbo.Artists", "Name", unique: true, name: "UX_Artist_Name");
            CreateIndex("dbo.Genres", "Name", unique: true, name: "UX_Genre_Name");
            AddForeignKey("dbo.Albums", "ArtistId", "dbo.Artists", "ArtistId");
            AddForeignKey("dbo.Albums", "GenreId", "dbo.Genres", "GenreId");
        }

        public override void Down()
        {
            DropForeignKey("dbo.Albums", "GenreId", "dbo.Genres");
            DropForeignKey("dbo.Albums", "ArtistId", "dbo.Artists");
            DropIndex("dbo.Genres", "UX_Genre_Name");
            DropIndex("dbo.Artists", "UX_Artist_Name");
            AlterColumn("dbo.Genres", "Description", c => c.String());
            AlterColumn("dbo.Genres", "Name", c => c.String());
            AlterColumn("dbo.Artists", "Name", c => c.String());
            DropColumn("dbo.Genres", "RowVersion");
            DropColumn("dbo.Artists", "RowVersion");
            DropColumn("dbo.Albums", "RowVersion");
            AddForeignKey("dbo.Albums", "GenreId", "dbo.Genres", "GenreId", cascadeDelete: true);
            AddForeignKey("dbo.Albums", "ArtistId", "dbo.Artists", "ArtistId", cascadeDelete: true);
        }
    }
}

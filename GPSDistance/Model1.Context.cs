﻿//------------------------------------------------------------------------------
// <auto-generated>
//    這個程式碼是由範本產生。
//
//    對這個檔案進行手動變更可能導致您的應用程式產生未預期的行為。
//    如果重新產生程式碼，將會覆寫對這個檔案的手動變更。
// </auto-generated>
//------------------------------------------------------------------------------

namespace GPSDistance
{
    using System;
    using System.Data.Entity;
    using System.Data.Entity.Infrastructure;
    
    public partial class MetroEntities : DbContext
    {
        public MetroEntities()
            : base("name=MetroEntities")
        {
        }
    
        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            throw new UnintentionalCodeFirstException();
        }
    
        public DbSet<線路> 線路 { get; set; }
        public DbSet<LineEdge> LineEdge { get; set; }
        public DbSet<Dijkstra> Dijkstra { get; set; }
        public DbSet<Trip> Trip { get; set; }
        public DbSet<TripNode> TripNode { get; set; }
        public DbSet<車站> 車站 { get; set; }
        public DbSet<轉乘> 轉乘 { get; set; }
    }
}

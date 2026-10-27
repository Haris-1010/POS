using System;
using System.Collections.Generic;

namespace CoreMedixBeta.Models.InventoryItems.DTOs
{
    public class NameIdLookupDto
    {
        public int ID { get; set; }
        public string Name { get; set; }
    }

    public class VariationNameLookupDto
    {
        public int ItemID { get; set; }
        public string VariationName { get; set; }
    }

    public class ImportItemsInputDto
    {
        public List<ImportItemRowDto> Rows { get; set; } = new List<ImportItemRowDto>();
    }

    public class ImportItemRowDto
    {
        public string RowNumber { get; set; }
        public string ItemName { get; set; }
        public string ProductType { get; set; }
        public string MedicineSalt { get; set; }
        public string ManufacturerName { get; set; }
        public string SupplierName { get; set; }
        public string VariationName { get; set; }
        public string SKU { get; set; }
        public string ExpiryDate { get; set; }
        public string OpeningStock { get; set; }
        public string CostPrice { get; set; }
        public string SellingPrice { get; set; }
        public string OpeningStockRate { get; set; }
        public string ReorderPoint { get; set; }
        public string Tax { get; set; }
        public string Batch { get; set; }
        public string NoOfPillsInPack { get; set; }
    }

    public class ImportItemsOutputDto
    {
        public ImportItemsOutputDto()
        {
            Errors = new List<string>();
        }

        public int TotalRows { get; set; }
        public int ItemsCreated { get; set; }
        public int ItemsReused { get; set; }
        public int VariationsCreated { get; set; }
        public int Skipped { get; set; }
        public int Failed { get; set; }
        public int ManufacturersCreated { get; set; }
        public int SuppliersCreated { get; set; }
        public List<string> Errors { get; set; }
    }

    public class BulkDeleteItemsInputDto
    {
        public BulkDeleteItemsInputDto()
        {
            IDs = new List<int>();
            Identifiers = new List<string>();
        }

        public List<int> IDs { get; set; }
        public List<string> Identifiers { get; set; }
    }

    public class BulkDeleteItemsOutputDto
    {
        public int DeletedItems { get; set; }
        public int NotMatched { get; set; }
    }

    public class BulkItemsInputDto
    {
        public string Search { get; set; }
        public int ManufacturerID { get; set; }
    }

    public class BulkItemKeyDto
    {
        public string ItemName { get; set; }
        public string VariationName { get; set; }
        public string UniqueID { get; set; }
        public string Qty { get; set; }
    }

    public class ResolveBulkItemsInputDto
    {
        public ResolveBulkItemsInputDto()
        {
            Keys = new List<BulkItemKeyDto>();
        }

        public List<BulkItemKeyDto> Keys { get; set; }
    }

    public class ResolveBulkItemsOutputDto
    {
        public ResolveBulkItemsOutputDto()
        {
            Items = new List<SearchItemsOutputDto>();
            Unresolved = new List<string>();
        }

        public List<SearchItemsOutputDto> Items { get; set; }
        public List<string> Unresolved { get; set; }
    }
}

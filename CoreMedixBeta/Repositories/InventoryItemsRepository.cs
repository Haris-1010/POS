using CoreMedixBeta.DbModels;
using CoreMedixBeta.Models;
using CoreMedixBeta.Models.InventoryItems;
using CoreMedixBeta.Models.InventoryItems.DTOs;
using Dapper;
using CoreMedixBeta.Models.ManufacturerBrand;
using CoreMedixBeta.Models.Menufacturer;
using CoreMedixBeta.Models.Patient;
using CoreMedixBeta.Models.Supplier;
using InstaCareEMR.Core.DBHelpers;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Configuration;
using System.Globalization;
using System.Linq;
using System.Text;
using ErrorOr;
using System.Threading.Tasks;
using System.Web;

namespace CoreMedixBeta.Repositories
{
    public class InventoryItemsRepository
    {
        public async Task<bool> DeleteItemAsync(DeleteItemInputDto input)
        {
            try
            {
                Dictionary<string, string> dbValues = new Dictionary<string, string>
                {
                    {"@ID", input.ID.ToString() },
                    {"@DeletedBy", input.DeletedBy },
                    {"@DeletedOn", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") },
                };

                string query = @"UPDATE Inventory.InventoryItems SET IsDeleted = 1, DeletedBy = @DeletedBy, DeletedOn = @DeletedOn WHERE ID = @ID;
                               DELETE FROM Inventory.InventoryItemVariations WHERE ItemID = @ID;";

                return await DBHelper.ExecuteQueryAsync(query, dbValues);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return false;
            }
        }

        public async Task<InventoryItem> GetItemByIdAsync(GetItemByIdInputDto input)
        {
            try
            {
                Dictionary<string, string> dbValues = new Dictionary<string, string>
                {
                    { "@ID", input.ID.ToString() },
                };
                string query = @"SELECT ID, Name,ProductType,MedicineSalt,ManufacturerID,ManufacturerID,SupplierID, BrandID
                                FROM Inventory.InventoryItems
                                WHERE IsDeleted = 0 AND ID = @ID ";
                return await DBHelper.GetAsync<InventoryItem>(query, Params: dbValues);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return new InventoryItem();
            }
        }

        public async Task<Tuple<string, string>> GetPagedItemsAsync(GetPagedItemsInputDto input)
        {
            try
            { 
                Dictionary<string, string> dbValues = new Dictionary<string, string>
                {
                };

                DataTableValuesDTO dataTableValues = input.DataTableValues;

                string query = @" SELECT I.ID, I.Name AS ProductName, I.ProductType, M.Name AS ManufacturerName, I.ManufacturerID,
                      STUFF((SELECT ', ' + S2.Name FROM Supplier.Suppliers S2
                   WHERE ',' + CAST(I.SupplierID AS NVARCHAR(MAX)) + ',' LIKE '%,' + CAST(S2.ID AS NVARCHAR) + ',%'

                      FOR XML PATH(''), TYPE).value('.', 'NVARCHAR(MAX)'), 1, 2, '') AS SupplierName,
                                COUNT(V.ID) AS TotalVariations,ISNULL(SUM(V.OpeningStock),0) AS VariationStockQty,
                                COUNT(CASE WHEN V.ExpiryDate <= DATEADD(DAY, 7, GETDATE()) THEN 1 END) AS NearToExpireVariations,
                                COUNT(CASE WHEN V.OpeningStock <= V.ReorderPoint THEN 1 END) AS NearToFinishVariations,
                                CASE WHEN I.CreatedOn >= DATEADD(DAY, -15, GETDATE()) THEN 1 ELSE 0 END AS NewArrivals,
                                CASE WHEN COUNT(V.ID) = 1 THEN MIN(V.UniqueID) ELSE '' END AS UniqueID
                                FROM Inventory.InventoryItems I
                                LEFT JOIN Inventory.InventoryItemVariations V ON V.ItemID = I.ID
                                LEFT JOIN Manufacturer.Manufacturers M ON M.ID = I.ManufacturerID AND M.IsDeleted = 0
                                WHERE  I.IsDeleted = 0 
                ";
                

                if (!string.IsNullOrEmpty(input.FilterType))
                {
                    switch (input.FilterType)
                    {
                        case "LowStock":
                            query += " AND EXISTS (SELECT 1 FROM Inventory.InventoryItemVariations VX WHERE VX.ItemID = I.ID AND VX.OpeningStock <= VX.ReorderPoint) ";
                            break;

                        case "ExpiringSoon":
                            query += " AND EXISTS (SELECT 1 FROM Inventory.InventoryItemVariations VX WHERE VX.ItemID = I.ID AND VX.ExpiryDate <= DATEADD(DAY, 7, GETDATE())) ";
                            break;

                        case "NewArrival":
                            query += " AND I.CreatedOn >= DATEADD(DAY, -15, GETDATE()) ";
                            break;

                        case "All":
                        default:
                            // No additional filter
                            break;
                    }
                }

                if (input.SKU > 0)
                {
                    dbValues.Add("@SKU", input.SKU.ToString());
                    query += " AND EXISTS (SELECT 1 FROM Inventory.InventoryItemVariations VX WHERE VX.ItemID = I.ID AND VX.SKU = @SKU) ";
                }

                if (!string.IsNullOrEmpty(input.UPC))
                {
                    dbValues.Add("@UPC", input.UPC);
                    query += " AND EXISTS (SELECT 1 FROM Inventory.InventoryItemVariations VX WHERE VX.ItemID = I.ID AND VX.UPC = @UPC) ";
                }

                if (!string.IsNullOrEmpty(input.ProductType))
                {
                    dbValues.Add("@ProductType", input.ProductType);
                    query += " AND I.ProductType = @ProductType ";
                }

                if (!string.IsNullOrEmpty(input.UniqueID))
                {
                    dbValues.Add("@UniqueID", input.UniqueID);
                    query += " AND EXISTS (SELECT 1 FROM Inventory.InventoryItemVariations VX WHERE VX.ItemID = I.ID AND VX.UniqueID = @UniqueID) ";
                }

                if (input.SupplierID > 0)
                {
                    dbValues.Add("@SupplierID", input.SupplierID.ToString());
                    query += " AND ',' + CAST(I.SupplierID AS NVARCHAR(MAX)) + ',' LIKE '%,' + CAST(@SupplierID AS NVARCHAR) + ',%' ";
                }

                if (!string.IsNullOrEmpty(dataTableValues.SearchString))
                {
                    dbValues.Add("@SearchString", $"%{dataTableValues.SearchString}%");
                    query += " AND (I.Name LIKE @SearchString OR EXISTS (SELECT 1 FROM Inventory.InventoryItemVariations VX WHERE VX.ItemID = I.ID AND VX.VariationName LIKE @SearchString)) ";
                }

                if (input.ManufacturerID > 0)
                {
                    dbValues.Add("@ManufacturerID", input.ManufacturerID.ToString());
                    query += " AND I.ManufacturerID = @ManufacturerID ";
                }

                query += " GROUP BY I.ID, I.Name, I.ProductType, M.Name, I.SupplierID, I.ManufacturerID, I.CreatedOn ";

                int p = dataTableValues.Offset / dataTableValues.PageSize;
                Dictionary<string, string> sortAndOrder = new Dictionary<string, string>
                {
                    {
                        $"MyList.{(string.IsNullOrEmpty(input.SortBy) ? "ID" : input.SortBy)}",
                        string.IsNullOrEmpty(input.SortByOrder) ? "DESC" : input.SortByOrder
                    }
                };

                var data = await DBHelper.GetPagedListAsync<GetPagedItemsOutputDto>(query, sortAndOrder, dataTableValues.PageSize, p, dbValues);
                return Tuple.Create(JsonConvert.SerializeObject(data.Results), JsonConvert.SerializeObject(data.pager.TotalRecords));
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return Tuple.Create(string.Empty, string.Empty);
            }
        }

        public async Task<int> SaveItemAsync(InventoryItem input)
        {
            try
            {
                if (input.ID == 0)
                { 
                    if (string.IsNullOrWhiteSpace(input.CreatedBy)) input.CreatedBy = "System";
                    input.CreatedOn = DateTime.Now;
                    return int.Parse(await DBHelper.InsertAndGetIdAsync(input));
                }
                else
                {
                    input.LastModifiedOn = DateTime.Now;
                    if (string.IsNullOrWhiteSpace(input.LastModifiedBy)) input.LastModifiedBy = "System";

                    bool isUpdated = await DBHelper.UpdateAsync(input, "WHERE ID = @ID", new Dictionary<string, string>
                    {
                        {"@ID", input.ID.ToString() }
                    });

                    if (isUpdated) return input.ID;
                    else return 0;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return 0;
            }
        }

        public async Task<ErrorOr<int>> SaveItemVariationAsync(InventoryItemVariation input)
        {
            try
            {
                if (input.ID == 0)
                {
                    input.CreatedOn = DateTime.Now;
                    input.UniqueID = GenerateShortGuid(6);
                    if (string.IsNullOrWhiteSpace(input.CreatedBy)) input.CreatedBy = "System";
                    var idString = await DBHelper.InsertAndGetIdAsync(input);

                    if (int.TryParse(idString, out int newId))
                        return newId;

                    return Error.Failure("ItemVariation.InsertFailed", "Failed to insert item variation.");
                }
                else
                {
                    input.LastModifiedOn = DateTime.Now;
                    if (string.IsNullOrWhiteSpace(input.LastModifiedBy)) input.LastModifiedBy = "System";
                    
                    bool isUpdated = await DBHelper.UpdateAsync(
                        input,
                        "WHERE ID = @ID",
                        new Dictionary<string, string> { { "@ID", input.ID.ToString() } }
                    );

                    if (isUpdated)
                        return input.ID;

                    return Error.Failure("ItemVariation.UpdateFailed", "Failed to update item variation.");
                }
            }
            catch (Exception ex)
            {
                return Error.Failure("ItemVariation.Exception", ex.Message);
            }
        }

        public async Task UpdateProductIDs()
        {
            using (var connection = new SqlConnection(ConfigurationManager.ConnectionStrings["NewConnectionString"].ToString()))
            {
                await connection.OpenAsync();

                // Step 1: Get all variation IDs with missing or short UniqueID
                var ids = (await connection.QueryAsync<int>(
                    @"SELECT Id FROM Inventory.InventoryItemVariations
              WHERE UniqueID IS NULL OR LEN(UniqueID) < 6")).ToList();

                // Step 2: Loop and update each with a unique 6-character ID
                foreach (var id in ids)
                {
                    string uniqueId = GenerateShortGuid(6);

                    // Optional: Make sure it's not already used (you can optimize this)
                    var exists = await connection.ExecuteScalarAsync<int>(
                        "SELECT COUNT(*) FROM Inventory.InventoryItemVariations WHERE UniqueID = @UniqueID",
                        new { UniqueID = uniqueId });

                    if (exists > 0)
                    {
                        uniqueId = GenerateShortGuid(6); // re-generate if duplicate
                    }

                    await connection.ExecuteAsync(
                        @"UPDATE Inventory.InventoryItemVariations
                  SET UniqueID = @UniqueID WHERE Id = @Id",
                        new { UniqueID = uniqueId, Id = id });
                }
            }
        }

        private string GenerateShortGuid(int length)
        {
            return Guid.NewGuid().ToString("N").Substring(0, length);
        }

        public async Task<bool> DeleteVariationAsync(DeleteInventoryItemInputDto input)
        {
            try
            {
                Dictionary<string, string> dbValues = new Dictionary<string, string>
                {
                    {"@ID", input.ID.ToString() },
                };
                string query = @"DELETE FROM Inventory.InventoryItemVariations WHERE ID = @ID";
                return await DBHelper.ExecuteQueryAsync(query, dbValues);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return false;
            }
        }

        public async Task<Tuple<string, string>> GetPagedItemVariationsAsync(GetPagedItemVariationsInputDto input)
        {
            try
            {
                Dictionary<string, string> dbValues = new Dictionary<string, string>
                {
                    { "@ItemID", input.ItemID.ToString() }
                };

                DataTableValuesDTO dataTableValues = input.DataTableValues;

                string query = @"SELECT V.ID, V.ItemID, V.VariationName, V.SKU, V.UPC, V.OpeningStock, V.MRP,
                               V.OpeningStockRate, V.ReorderPoint, V.Batch, V.Weight, V.ExpiryDate, 
                               V.Tax, V.IsReturnable, V.Description, V.SellingPrice, V.CostPrice,V.Margin,V.NoOfPillsInPack,
                               F.Title AS Formation, C.Title AS Category, SC.Title AS SubCategory, U.Title AS Unit, WU.Title AS WeightUnit
                               FROM Inventory.InventoryItemVariations V
                               LEFT JOIN Inventory.ItemAttributes F ON F.ID = V.FormationID AND F.AttributeType = 'Formation'
                               LEFT JOIN Inventory.ItemAttributes C ON C.ID = V.CategoryID AND C.AttributeType = 'Category'
                               LEFT JOIN Inventory.ItemAttributes SC ON SC.ID = V.SubCategoryID AND SC.AttributeType = 'Subcategory'
                               LEFT JOIN Inventory.ItemAttributes U ON U.ID = V.UnitID AND U.AttributeType = 'Unit'
                               LEFT JOIN Inventory.ItemAttributes WU ON WU.ID = V.WeightUnitID AND WU.AttributeType = 'WeightUnit'
                               WHERE V.ItemID = @ItemID ";

                string countQuery = @"SELECT COUNT(*) FROM Inventory.InventoryItemVariations V WHERE V.ItemID = @ItemID ";

                if (!string.IsNullOrEmpty(dataTableValues.SearchString))
                {
                    dbValues.Add("@SearchString", $"%{dataTableValues.SearchString}%");
                    query += " AND V.VariationName LIKE @SearchString ";
                    countQuery += " AND V.VariationName LIKE @SearchString ";
                }

                int p = dataTableValues.Offset / dataTableValues.PageSize;
                Dictionary<string, string> sortAndOrder = new Dictionary<string, string>
                {
                    { "MyList.ID", "DESC" }
                };

                var data = await DBHelper.GetPagedListAsync<GetPagedItemVariationsOutputDto>(query, sortAndOrder, dataTableValues.PageSize, p, dbValues, countQuery);
                return Tuple.Create(JsonConvert.SerializeObject(data.Results), JsonConvert.SerializeObject(data.pager.TotalRecords));
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return Tuple.Create(string.Empty, string.Empty);
            }
        }

        public async Task<List<GetAllAttributesOutputDto>> GetAllAttributesAsync(ManageItemInputDto input)
        {
            try
            {
                Dictionary<string, string> dbValues = new Dictionary<string, string>
                {
                };
                string query = @"SELECT ID, Title AS Name, AttributeType AS Type FROM Inventory.ItemAttributes ";

                if (!string.IsNullOrWhiteSpace(input.Type))
                {
                    dbValues.Add("@Type", input.Type);
                    query += " AND Type = @Type ";
                }

                return await DBHelper.GetListAsync<GetAllAttributesOutputDto>(query, Params: dbValues);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return new List<GetAllAttributesOutputDto>();
            }
        }

        public async Task<InventoryItemVariation> GetVariationByIdAsync(GetItemByIdInputDto input)
        {
            try
            {
                string query = @"SELECT TOP 1 * FROM Inventory.InventoryItemVariations WHERE ID = @ID";
                return await DBHelper.GetAsync<InventoryItemVariation>(query, Params: new Dictionary<string, string>
                {
                    {"@ID", input.ID.ToString() }
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return new InventoryItemVariation();
            }
        }

        public async Task<bool> SaveVariationImagesAsync(List<InventoryItemVariationImage> images)
        {
            try
            {
                int variationID = images.First().VariationID;
                Dictionary<string, string> dbValues = new Dictionary<string, string>
                {
                    {"@VariationID", variationID.ToString() }
                };

                string deleteQuery = @"DELETE FROM Inventory.InventoryItemVariationImages WHERE VariationID = @VariationID";
                bool isDeleted = await DBHelper.ExecuteQueryAsync(deleteQuery, dbValues);

                return await DBHelper.InsertListAsync(images);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return false;
            }
        }

        public async Task<List<InventoryItemVariationImage>> GetVariationImagesAsync(GetItemByIdInputDto input)
        {
            try
            {
                Dictionary<string, string> dbValues = new Dictionary<string, string>
                {
                    {"@VariationID", input.ID.ToString() }
                };

                string query = @"SELECT * FROM Inventory.InventoryItemVariationImages WHERE VariationID = @VariationID";
                return await DBHelper.GetListAsync<InventoryItemVariationImage>(query, Params: dbValues);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return new List<InventoryItemVariationImage>();
            }
        }

        public async Task<List<SearchItemsOutputDto>> SearchItemsAsync(SearchItemsInputDto input)
        {
            try
            {
                Dictionary<string, string> dbValues = new Dictionary<string, string>
                {
                };

                string query = string.Empty;
                 

                if (!string.IsNullOrWhiteSpace(input.SearchType))
                {
                    query = @"
        SELECT TOP 10 
            V.ID AS VariationID, 
            V.VariationName, 
            I.ID AS ItemID, 
            I.Name AS ItemName,
            V.OpeningStock AS Quantity, 
            V.SKU, 
            V.UPC, 
            V.MRP,
            V.OpeningStockRate, 
            V.ReorderPoint, 
            V.Batch, 
            V.Weight, 
            V.ExpiryDate,  
            IAF.Title AS Formation,
            V.Tax, 
            V.IsReturnable, 
            V.Description, 
            V.SellingPrice, 
            V.CostPrice,
            V.Margin,
            CASE WHEN V.ExpiryDate < GETDATE() THEN 'Expired' ELSE 'Not Expired' END AS ExpiryStatus
        FROM Inventory.InventoryItemVariations V
        JOIN Inventory.InventoryItems I ON V.ItemID = I.ID
        LEFT JOIN Inventory.ItemAttributes IAF 
               ON IAF.ID = V.FormationID 
              AND IAF.AttributeType = 'Formation'
        WHERE I.IsDeleted = 0 ";

                    if (!string.IsNullOrEmpty(input.SearchString))
                    {
                        dbValues.Add("@SearchString", $"%{input.SearchString}%");

                        switch (input.SearchType)
                        {
                            case "Variation":
                                query += " AND (V.VariationName LIKE @SearchString OR V.UPC LIKE @SearchString) ";
                                break;

                            case "Salt":
                                query += @"
                                AND (
                                    I.MedicineSalt LIKE @SearchString
                                    OR EXISTS (
                                        SELECT 1
                                        FROM STRING_SPLIT(I.MedicineSalt, ',')
                                        WHERE value LIKE @SearchString
                                    )
                                )";
                                            break;

                            case "Alternate":
                                query += @"
                                    AND (
                                        LOWER(LTRIM(RTRIM(V.VariationName))) LIKE LOWER(@SearchString)
                                        OR LOWER(LTRIM(RTRIM(V.VariationName))) IN (
                                            SELECT LOWER(LTRIM(RTRIM(value)))
                                            FROM Inventory.InventoryItemVariations PIV
                                            CROSS APPLY STRING_SPLIT(PIV.AlternateItems, ',')
                                            JOIN Inventory.InventoryItems PII ON PIV.ItemID = PII.ID
                                            WHERE PII.IsDeleted = 0
                                              AND value LIKE @SearchString
                                        )
                                    )";
                                break;


                            default:
                                query += " AND (I.Name LIKE @SearchString OR V.UPC LIKE @SearchString) ";
                                break;
                        }
                    }
                }
                else
                {
                    query = @"
        SELECT TOP 10 
            I.ID AS ItemID, 
            I.Name AS ItemName, 
            V.ID AS VariationID, 
            V.VariationName,
            V.OpeningStock AS Quantity, 
            V.SKU, 
            V.UPC, 
            V.MRP,
            V.OpeningStockRate, 
            V.ReorderPoint, 
            V.Batch, 
            V.Weight, 
            V.ExpiryDate,  
            IAF.Title AS Formation,
            V.Tax, 
            V.IsReturnable, 
            V.Description, 
            V.SellingPrice, 
            V.CostPrice,
            V.Margin,
            CASE WHEN V.ExpiryDate < GETDATE() THEN 'Expired' ELSE 'Not Expired' END AS ExpiryStatus
        FROM Inventory.InventoryItems I
        JOIN Inventory.InventoryItemVariations V ON I.ID = V.ItemID
        LEFT JOIN Inventory.ItemAttributes IAF 
               ON IAF.ID = V.FormationID 
              AND IAF.AttributeType = 'Formation'
        WHERE I.IsDeleted = 0 ";

                    if (!string.IsNullOrEmpty(input.SearchString))
                    {
                        dbValues.Add("@SearchString", $"%{input.SearchString}%");

                        query += @"
            AND (
                I.Name LIKE @SearchString
                OR V.VariationName LIKE @SearchString
                OR V.UPC LIKE @SearchString
                OR EXISTS (
                    SELECT 1
                    FROM STRING_SPLIT(I.MedicineSalt, ',')
                    WHERE value LIKE @SearchString
                )
                OR V.VariationName IN (
                    SELECT value
                    FROM STRING_SPLIT(
                        ISNULL((
                            SELECT TOP 1 PIV.AlternateItems
                            FROM Inventory.InventoryItemVariations PIV
                            JOIN Inventory.InventoryItems PII ON PIV.ItemID = PII.ID
                            WHERE PII.IsDeleted = 0
                              AND PIV.VariationName LIKE @SearchString
                        ), ''), ','
                    )
                )
            )";
                    }
                }

                if (input.ManufacturerID > 0)
                {
                    dbValues.Add("@ManufacturerID", input.ManufacturerID.ToString());
                    query += " AND I.ManufacturerID = @ManufacturerID ";
                }

                if (input.SupplierID > 0)
                {
                    dbValues.Add("@SupplierID", input.SupplierID.ToString());
                    query += " AND I.SupplierID = @SupplierID ";
                }


                if (input.NearToFinish)
                {
                    query += " AND (V.OpeningStock <= V.ReorderPoint AND V.OpeningStock > 0) ";
                }

                if (input.NearToExpire)
                {
                    dbValues.Add("@TommorrowDate", ServerConfig.DateTimeNow.AddDays(1).ToString());
                    dbValues.Add("@NextWeekDate", ServerConfig.DateTimeNow.AddDays(8).ToString());
                    query += " AND V.ExpiryDate BETWEEN @TommorrowDate AND @NextWeekDate ";
                }

                if (input.Finished)
                {
                    query += " AND V.OpeningStock <= 0 ";
                }

                if (input.Expired)
                {
                    dbValues.Add("@CurrentDate", ServerConfig.DateTimeNow.ToString());
                    query += " AND V.ExpiryDate <= @CurrentDate ";
                }

                query += " ORDER BY I.Name, V.VariationName ";


                return await DBHelper.GetListAsync<SearchItemsOutputDto>(query, Params: dbValues);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return new List<SearchItemsOutputDto>();
            }
        }

        public async Task<List<SearchItemsOutputDto>> GetItemsByReferralOrderAsync(SearchItemsInputDto input)
        {
            try
            {
                Dictionary<string, string> dbValues = new Dictionary<string, string>
                { 
                };

                string query = @"SELECT I.ID AS ItemID, I.Name AS ItemName, V.ID AS VariationID, V.VariationName AS VariationName,
                            V.OpeningStock AS Quantity, V.SKU, V.UPC, V.OpeningStock, V.MRP,
                            V.OpeningStockRate, V.ReorderPoint, V.Batch, V.Weight, V.ExpiryDate, 
                            V.Tax, V.IsReturnable, V.Description, V.SellingPrice, V.CostPrice,V.Margin,
                            CASE
                                WHEN V.ExpiryDate < GETDATE() THEN 'Expired'
                                ELSE 'Not Expired'
                            END AS ExpiryStatus
                            FROM Pharmacy.ReferralOrderItems R
                            JOIN Inventory.InventoryItems I ON R.ItemID = I.ID
                            JOIN Inventory.InventoryItemVariations V ON R.VariationID = V.ID
                            WHERE R.OrderID = @ReferralOrderID";

                return await DBHelper.GetListAsync<SearchItemsOutputDto>(query, Params: dbValues);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return new List<SearchItemsOutputDto>();
            }
        }

        public async Task<string> GenerateUniqueBarcodeAsync()
        {
            string barcode;
            var rand = new Random();

            do
            {
                barcode = "999" + rand.Next(100000000, 999999999); // 12-digit barcode
            }
            while (await DBHelper.ExecuteQueryAsync(
                "SELECT COUNT(1) FROM Inventory.InventoryItemVariations WHERE UPC = @UPC",
                new Dictionary<string, string> { { "@UPC", barcode } }));

            return barcode;
        }

        public async Task<GetDashBoardCardsOutputDto> GetInventoryItmesInfoAsync()
        {
            try
            {
                Dictionary<string, string> dbValues = new Dictionary<string, string>
                {
                };

                string query = @" SELECT COUNT(V.ID) TotalItems,
                              COUNT(CASE WHEN V.ExpiryDate < CAST(GETDATE() AS DATE) THEN 1 END) AS TotalExpiryItems,
                              COUNT(CASE WHEN V.CreatedOn >= DATEADD(DAY, -15, GETDATE()) THEN V.ID END) NewArrivalItems,
                              COUNT(CASE WHEN V.ExpiryDate <= DATEADD(DAY, 7, GETDATE()) THEN 1 END) TotalExpirySoonItems,
                              COUNT(CASE WHEN V.OpeningStock <= V.ReorderPoint THEN 1 END) LowStockItems
                              FROM Inventory.InventoryItemVariations V
                              INNER JOIN Inventory.InventoryItems I ON I.ID = V.ItemID
                              WHERE I.IsDeleted = 0 ";

                return await DBHelper.GetAsync<GetDashBoardCardsOutputDto>(query, Params: dbValues);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return new GetDashBoardCardsOutputDto();
            }
        }

        public async Task<List<GetInventoryItemLogsOutputDto>> GetListItemActivityLogsAsync(GetItemActivityLogInputDto input)
        {
            try
            {
                Dictionary<string, string> dbValues = new Dictionary<string, string>
                {
                   { "@VariationID", input.VariationID.ToString() },
                };

                string query = @"SELECT
                    AT.ID,
                    AT.TimeStamp,
                    AT.EventType,
                    AT.[Table],
                    AT.Description,
                    AT.AffectedID,
                    Source.LogType,

                    -- Variation Name / Item Name
                    CASE
                        WHEN Source.LogType = 'InventoryItemVariation' THEN JSON_VALUE(Source.value, '$.VariationName')
                        ELSE JSON_VALUE(Source.value, '$.Name')
                    END AS VariationName,

                    -- Qty / OpeningStock
                    CAST(
                        CASE
                            WHEN Source.LogType = 'InventoryItemVariation' THEN JSON_VALUE(Source.value, '$.OpeningStock')
                            ELSE JSON_VALUE(Source.value, '$.Qty')
                        END AS DECIMAL(18,2)
                    ) AS Qty,

                    -- UnitCost / SellingPrice
                    CAST(
                        CASE
                            WHEN Source.LogType = 'InventoryItemVariation' THEN JSON_VALUE(Source.value, '$.SellingPrice')
                            ELSE JSON_VALUE(Source.value, '$.UnitCost')
                        END AS DECIMAL(18,2)
                    ) AS UnitCost,

                    -- Discount
                    CAST(JSON_VALUE(Source.value, '$.Discount') AS DECIMAL(18,2)) AS Discount,

                    AT.UserID,
                    IU.Name AS UserName,
                    AT.IPAddress

                FROM
                    Pharmacy.AuditTrails AS AT
                LEFT JOIN InstaUser.[User] AS IU ON IU.Username = AT.UserID

                CROSS APPLY (
                    SELECT 'PurchaseOrders.OrderItems' AS LogType, value
                    FROM OPENJSON(AT.[Values], '$.PurchaseOrders.OrderItems')
                    WHERE JSON_VALUE(value, '$.VariationID') = @VariationID

                    UNION ALL
                    SELECT 'Purchases.PurchaseItems', value
                    FROM OPENJSON(AT.[Values], '$.Purchases.PurchaseItems')
                    WHERE JSON_VALUE(value, '$.VariationID') = @VariationID

                    UNION ALL
                    SELECT 'PrintPurchaseOrders.DetailedOrder.OrderItems', value
                    FROM OPENJSON(AT.[Values], '$.PrintPurchaseOrders.DetailedOrder.OrderItems')
                    WHERE JSON_VALUE(value, '$.VariationID') = @VariationID

                    UNION ALL
                    SELECT 'PrintPurchases.Purchase.PurchaseItems', value
                    FROM OPENJSON(AT.[Values], '$.PrintPurchases.Purchase.PurchaseItems')
                    WHERE JSON_VALUE(value, '$.VariationID') = @VariationID

                    UNION ALL
                    SELECT 'SaleInvoice.SaleInvoiceItems', value
                    FROM OPENJSON(AT.[Values], '$.SaleInvoice.SaleInvoiceItems')
                    WHERE JSON_VALUE(value, '$.VariationID') = @VariationID

                    UNION ALL
                    SELECT 'SaleInvoice.Invoice.SaleInvoiceItems', value
                    FROM OPENJSON(AT.[Values], '$.SaleInvoice.Invoice.SaleInvoiceItems')
                    WHERE JSON_VALUE(value, '$.VariationID') = @VariationID

                    UNION ALL
                    SELECT 'InventoryItemVariation', JSON_QUERY(AT.[Values], '$.InventoryItemVariation')
                    WHERE JSON_VALUE(AT.[Values], '$.InventoryItemVariation.ID') = @VariationID
                ) AS Source
                WHERE
                    ISJSON(AT.[Values]) = 1
                   ";

                query += " ORDER BY TimeStamp DESC ";

                return await DBHelper.GetListAsync<GetInventoryItemLogsOutputDto>(query, Params: dbValues);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return new List<GetInventoryItemLogsOutputDto>();
            }
        }

        #region Bulk Operations

        public async Task<ImportItemsOutputDto> ImportItemsAsync(List<ImportItemRowDto> rows, string username)
        {
            var output = new ImportItemsOutputDto();
            output.TotalRows = rows == null ? 0 : rows.Count;
            if (output.TotalRows == 0) return output;
            if (string.IsNullOrWhiteSpace(username)) username = "System";

            var manufacturers = await GetNameIdLookupAsync("SELECT ID, Name FROM Manufacturer.Manufacturers WHERE ISNULL(IsDeleted, 0) = 0");
            var suppliers = await GetNameIdLookupAsync("SELECT ID, Name FROM Supplier.Suppliers WHERE ISNULL(IsDeleted, 0) = 0");
            var items = await GetNameIdLookupAsync("SELECT ID, Name FROM Inventory.InventoryItems WHERE ISNULL(IsDeleted, 0) = 0");
            var variations = await GetVariationNameLookupAsync();

            int fallbackSupplierID = suppliers.Count > 0 ? suppliers.Values.First() : 0;
            int rowIndex = 1;

            foreach (var row in rows)
            {
                rowIndex++;
                if (row == null) continue;

                string label = string.IsNullOrWhiteSpace(row.RowNumber) ? rowIndex.ToString() : row.RowNumber.Trim();

                try
                {
                    string itemName = (row.ItemName ?? string.Empty).Trim();
                    string variationName = (row.VariationName ?? string.Empty).Trim();
                    string manufacturerName = (row.ManufacturerName ?? string.Empty).Trim();
                    string supplierName = (row.SupplierName ?? string.Empty).Trim();

                    if (itemName.Length == 0) { output.Failed++; AddImportError(output, label, "Item Name is required"); continue; }
                    if (variationName.Length == 0) { output.Failed++; AddImportError(output, label, "Variation Name is required"); continue; }
                    if (manufacturerName.Length == 0) { output.Failed++; AddImportError(output, label, "Manufacturer is required"); continue; }

                    DateTime expiryDate;
                    if (!TryParseImportDate(row.ExpiryDate, out expiryDate)) { output.Failed++; AddImportError(output, label, "Expiry Date is invalid (use e.g. 31-Dec-2027)"); continue; }

                    decimal costPrice;
                    if (!TryParseImportDecimal(row.CostPrice, out costPrice)) { output.Failed++; AddImportError(output, label, "Cost Price is required and must be a number"); continue; }

                    decimal sellingPrice;
                    if (!TryParseImportDecimal(row.SellingPrice, out sellingPrice)) { output.Failed++; AddImportError(output, label, "Selling Price is required and must be a number"); continue; }

                    decimal stock = 0;
                    if (!string.IsNullOrWhiteSpace(row.OpeningStock) && !TryParseImportDecimal(row.OpeningStock, out stock)) { output.Failed++; AddImportError(output, label, "Opening Stock must be a number"); continue; }

                    decimal reorderPoint = 0;
                    if (!string.IsNullOrWhiteSpace(row.ReorderPoint) && !TryParseImportDecimal(row.ReorderPoint, out reorderPoint)) { output.Failed++; AddImportError(output, label, "Reorder Point must be a number"); continue; }

                    decimal tax = 0;
                    if (!string.IsNullOrWhiteSpace(row.Tax) && !TryParseImportDecimal(row.Tax, out tax)) { output.Failed++; AddImportError(output, label, "Tax % must be a number"); continue; }

                    decimal stockRate = costPrice;
                    if (!string.IsNullOrWhiteSpace(row.OpeningStockRate) && !TryParseImportDecimal(row.OpeningStockRate, out stockRate)) { output.Failed++; AddImportError(output, label, "Opening Stock Rate must be a number"); continue; }

                    decimal pillsInPack = 1;
                    if (!string.IsNullOrWhiteSpace(row.NoOfPillsInPack) && !TryParseImportDecimal(row.NoOfPillsInPack, out pillsInPack)) { output.Failed++; AddImportError(output, label, "No. of Pills in Pack must be a number"); continue; }

                    decimal sku = 0;
                    if (!string.IsNullOrWhiteSpace(row.SKU) && !TryParseImportDecimal(row.SKU, out sku)) { output.Failed++; AddImportError(output, label, "SKU must be a number"); continue; }

                    int manufacturerID;
                    if (!manufacturers.TryGetValue(manufacturerName, out manufacturerID))
                    {
                        manufacturerID = await CreateManufacturerAsync(manufacturerName, username);
                        if (manufacturerID <= 0) { output.Failed++; AddImportError(output, label, "Unable to create manufacturer '" + manufacturerName + "'"); continue; }
                        manufacturers[manufacturerName] = manufacturerID;
                        output.ManufacturersCreated++;
                    }

                    int supplierID;
                    if (supplierName.Length == 0)
                    {
                        if (fallbackSupplierID <= 0)
                        {
                            supplierID = await CreateSupplierAsync("General Supplier", username);
                            if (supplierID <= 0) { output.Failed++; AddImportError(output, label, "Unable to resolve supplier"); continue; }
                            suppliers["General Supplier"] = supplierID;
                            fallbackSupplierID = supplierID;
                            output.SuppliersCreated++;
                        }
                        supplierID = fallbackSupplierID;
                    }
                    else if (!suppliers.TryGetValue(supplierName, out supplierID))
                    {
                        supplierID = await CreateSupplierAsync(supplierName, username);
                        if (supplierID <= 0) { output.Failed++; AddImportError(output, label, "Unable to create supplier '" + supplierName + "'"); continue; }
                        suppliers[supplierName] = supplierID;
                        output.SuppliersCreated++;
                    }

                    int itemID;
                    if (!items.TryGetValue(itemName, out itemID))
                    {
                        var itemEntity = new InventoryItem
                        {
                            Name = itemName,
                            ProductType = string.IsNullOrWhiteSpace(row.ProductType) ? "Medicine" : row.ProductType.Trim(),
                            MedicineSalt = (row.MedicineSalt ?? string.Empty).Trim(),
                            ManufacturerID = manufacturerID,
                            SupplierID = supplierID.ToString(),
                            CreatedBy = username
                        };

                        itemID = await SaveItemAsync(itemEntity);
                        if (itemID <= 0) { output.Failed++; AddImportError(output, label, "Unable to create item '" + itemName + "'"); continue; }

                        items[itemName] = itemID;
                        output.ItemsCreated++;
                    }
                    else
                    {
                        output.ItemsReused++;
                    }

                    HashSet<string> itemVariations;
                    if (!variations.TryGetValue(itemID, out itemVariations))
                    {
                        itemVariations = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                        variations[itemID] = itemVariations;
                    }

                    if (itemVariations.Contains(variationName)) { output.Skipped++; continue; }

                    var variationEntity = new InventoryItemVariation
                    {
                        ItemID = itemID,
                        VariationName = variationName,
                        SKU = (int)sku,
                        FormationID = 0,
                        UnitID = 0,
                        CategoryID = 0,
                        SubCategoryID = 0,
                        UPC = string.Empty,
                        OpeningStock = (int)stock,
                        ReorderPoint = (int)reorderPoint,
                        OpeningStockRate = stockRate,
                        NoOfPillsInPack = (int)pillsInPack,
                        Batch = (row.Batch ?? string.Empty).Trim(),
                        ExpiryDate = expiryDate.Date,
                        Tax = tax,
                        IsReturnable = true,
                        SellingPrice = sellingPrice,
                        CostPrice = costPrice,
                        CreatedBy = username
                    };

                    var saveResult = await SaveItemVariationAsync(variationEntity);
                    if (saveResult.IsError || saveResult.Value <= 0)
                    {
                        output.Failed++;
                        AddImportError(output, label, saveResult.IsError ? saveResult.FirstError.Description : "Unable to create variation '" + variationName + "'");
                        continue;
                    }

                    int variationID = saveResult.Value;
                    itemVariations.Add(variationName);

                    if (sku <= 0)
                    {
                        try
                        {
                            await DBHelper.ExecuteQueryAsync(
                                "UPDATE Inventory.InventoryItemVariations SET SKU = @ID WHERE ID = @ID",
                                new Dictionary<string, string> { { "@ID", variationID.ToString() } });
                        }
                        catch (Exception skuEx)
                        {
                            Console.WriteLine(skuEx.Message);
                        }
                    }

                    output.VariationsCreated++;
                }
                catch (Exception ex)
                {
                    output.Failed++;
                    AddImportError(output, label, ex.Message);
                }
            }

            return output;
        }

        public async Task<BulkDeleteItemsOutputDto> BulkDeleteItemsAsync(BulkDeleteItemsInputDto input, string username)
        {
            var output = new BulkDeleteItemsOutputDto();
            if (input == null) return output;
            if (string.IsNullOrWhiteSpace(username)) username = "System";

            var itemIDs = new SortedSet<int>();
            if (input.IDs != null)
            {
                foreach (var id in input.IDs)
                {
                    if (id > 0) itemIDs.Add(id);
                }
            }

            var identifiers = (input.Identifiers ?? new List<string>())
                .Select(x => (x ?? string.Empty).Trim())
                .Where(x => x.Length > 0)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (identifiers.Count > 0)
            {
                var matchedKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var rows = await GetItemIdsForIdentifiersAsync(identifiers);
                foreach (var row in rows)
                {
                    if (row.ID > 0) itemIDs.Add(row.ID);
                    if (!string.IsNullOrWhiteSpace(row.Name)) matchedKeys.Add(row.Name.Trim());
                }
                output.NotMatched = identifiers.Count - matchedKeys.Count;
            }

            if (itemIDs.Count == 0) return output;

            string idList = string.Join(",", itemIDs);
            string query = @"UPDATE Inventory.InventoryItems SET IsDeleted = 1, DeletedBy = @DeletedBy, DeletedOn = @DeletedOn
                             WHERE ID IN (" + idList + @") AND ISNULL(IsDeleted, 0) = 0;
                             DELETE FROM Inventory.InventoryItemVariations WHERE ItemID IN (" + idList + @");";

            await DBHelper.ExecuteQueryAsync(query, new Dictionary<string, string>
            {
                {"@DeletedBy", username },
                {"@DeletedOn", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") }
            });

            output.DeletedItems = itemIDs.Count;
            return output;
        }

        public async Task<List<SearchItemsOutputDto>> GetBulkItemsAsync(BulkItemsInputDto input)
        {
            try
            {
                var dbValues = new Dictionary<string, string>();
                string query = BulkItemSelectQuery + @" WHERE ISNULL(I.IsDeleted, 0) = 0 ";

                if (input != null && !string.IsNullOrWhiteSpace(input.Search))
                {
                    dbValues.Add("@Search", "%" + input.Search.Trim() + "%");
                    query += @" AND (I.Name LIKE @Search OR V.VariationName LIKE @Search OR V.UPC LIKE @Search OR V.UniqueID LIKE @Search) ";
                }

                if (input != null && input.ManufacturerID > 0)
                {
                    dbValues.Add("@ManufacturerID", input.ManufacturerID.ToString());
                    query += " AND I.ManufacturerID = @ManufacturerID ";
                }

                query += " ORDER BY I.Name, V.VariationName ";

                return await DBHelper.GetListAsync<SearchItemsOutputDto>(query, Params: dbValues);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return new List<SearchItemsOutputDto>();
            }
        }

        public async Task<ResolveBulkItemsOutputDto> ResolveBulkItemsAsync(List<BulkItemKeyDto> keys)
        {
            var output = new ResolveBulkItemsOutputDto();
            if (keys == null || keys.Count == 0) return output;

            var itemNames = keys.Select(x => (x.ItemName ?? string.Empty).Trim())
                                 .Where(x => x.Length > 0)
                                 .Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            var uniqueIDs = keys.Select(x => (x.UniqueID ?? string.Empty).Trim())
                                 .Where(x => x.Length > 0)
                                 .Distinct(StringComparer.OrdinalIgnoreCase).ToList();

            if (itemNames.Count == 0 && uniqueIDs.Count == 0)
            {
                foreach (var emptyKey in keys) output.Unresolved.Add(GetKeyLabel(emptyKey));
                return output;
            }

            var dbValues = new Dictionary<string, string>();
            var conditions = new List<string>();

            if (itemNames.Count > 0)
            {
                var paramNames = new List<string>();
                for (int i = 0; i < itemNames.Count; i++)
                {
                    string paramName = "@n" + i;
                    dbValues.Add(paramName, itemNames[i]);
                    paramNames.Add(paramName);
                }
                conditions.Add("I.Name IN (" + string.Join(",", paramNames) + ")");
            }

            if (uniqueIDs.Count > 0)
            {
                var paramNames = new List<string>();
                for (int i = 0; i < uniqueIDs.Count; i++)
                {
                    string paramName = "@u" + i;
                    dbValues.Add(paramName, uniqueIDs[i]);
                    paramNames.Add(paramName);
                }
                conditions.Add("V.UniqueID IN (" + string.Join(",", paramNames) + ")");
            }

            string query = BulkItemSelectQuery + @" WHERE ISNULL(I.IsDeleted, 0) = 0 AND (" + string.Join(" OR ", conditions) + ") ";
            var candidates = await DBHelper.GetListAsync<SearchItemsOutputDto>(query, Params: dbValues);

            var byUniqueID = new Dictionary<string, SearchItemsOutputDto>(StringComparer.OrdinalIgnoreCase);
            var byItemName = new Dictionary<string, List<SearchItemsOutputDto>>(StringComparer.OrdinalIgnoreCase);

            foreach (var candidate in candidates ?? new List<SearchItemsOutputDto>())
            {
                string candidateItemName = (candidate.ItemName ?? string.Empty).Trim();
                string candidateUniqueID = (candidate.UniqueID ?? string.Empty).Trim();

                if (candidateUniqueID.Length > 0 && !byUniqueID.ContainsKey(candidateUniqueID))
                {
                    byUniqueID[candidateUniqueID] = candidate;
                }

                if (!byItemName.ContainsKey(candidateItemName))
                {
                    byItemName[candidateItemName] = new List<SearchItemsOutputDto>();
                }
                byItemName[candidateItemName].Add(candidate);
            }

            foreach (var key in keys)
            {
                SearchItemsOutputDto match = null;
                string uniqueID = (key.UniqueID ?? string.Empty).Trim();
                string itemName = (key.ItemName ?? string.Empty).Trim();
                string variationName = (key.VariationName ?? string.Empty).Trim();

                SearchItemsOutputDto uniqueMatch;
                if (uniqueID.Length > 0 && byUniqueID.TryGetValue(uniqueID, out uniqueMatch))
                {
                    match = uniqueMatch;
                }
                else
                {
                    match = null;

                    List<SearchItemsOutputDto> itemCandidates;
                    if (itemName.Length > 0 && byItemName.TryGetValue(itemName, out itemCandidates))
                    {
                        if (variationName.Length > 0)
                        {
                            match = itemCandidates.FirstOrDefault(x =>
                                string.Equals((x.VariationName ?? string.Empty).Trim(), variationName, StringComparison.OrdinalIgnoreCase));
                        }
                        else if (itemCandidates.Count == 1)
                        {
                            match = itemCandidates[0];
                        }
                    }
                }

                if (match != null) output.Items.Add(match);
                else output.Unresolved.Add(GetKeyLabel(key));
            }

            return output;
        }

        private const string BulkItemSelectQuery = @"SELECT TOP 500
            I.ID AS ItemID,
            I.Name AS ItemName,
            V.ID AS VariationID,
            V.VariationName,
            V.UniqueID,
            V.OpeningStock AS Quantity,
            V.SKU,
            V.UPC,
            V.ReorderPoint,
            V.OpeningStockRate,
            V.Batch,
            V.ExpiryDate,
            V.Tax,
            V.SellingPrice,
            V.CostPrice,
            V.Margin,
            V.MRP,
            CASE WHEN V.ExpiryDate < GETDATE() THEN 'Expired' ELSE 'Not Expired' END AS ExpiryStatus
        FROM Inventory.InventoryItems I
        JOIN Inventory.InventoryItemVariations V ON V.ItemID = I.ID ";

        private static string GetKeyLabel(BulkItemKeyDto key)
        {
            if (key == null) return "Empty row";

            string uniqueID = (key.UniqueID ?? string.Empty).Trim();
            string itemName = (key.ItemName ?? string.Empty).Trim();
            string variationName = (key.VariationName ?? string.Empty).Trim();

            if (uniqueID.Length > 0) return "Unique ID " + uniqueID;
            if (itemName.Length > 0 && variationName.Length > 0) return itemName + " - " + variationName;
            if (itemName.Length > 0) return itemName;
            return "Empty row";
        }

        private async Task<List<NameIdLookupDto>> GetItemIdsForIdentifiersAsync(List<string> identifiers)
        {
            var dbValues = new Dictionary<string, string>();
            var paramNames = new List<string>();

            for (int i = 0; i < identifiers.Count; i++)
            {
                string paramName = "@k" + i;
                dbValues.Add(paramName, identifiers[i]);
                paramNames.Add(paramName);
            }

            string inClause = string.Join(",", paramNames);

            string query = @"
                SELECT DISTINCT I.ID, LOWER(LTRIM(RTRIM(I.Name))) AS Name
                FROM Inventory.InventoryItems I
                WHERE ISNULL(I.IsDeleted, 0) = 0
                  AND LOWER(LTRIM(RTRIM(I.Name))) IN (" + inClause + @")
                UNION
                SELECT DISTINCT I.ID, LOWER(LTRIM(RTRIM(V.UniqueID))) AS Name
                FROM Inventory.InventoryItemVariations V
                INNER JOIN Inventory.InventoryItems I ON I.ID = V.ItemID
                WHERE ISNULL(I.IsDeleted, 0) = 0
                  AND LOWER(LTRIM(RTRIM(V.UniqueID))) IN (" + inClause + @")";

            return await DBHelper.GetListAsync<NameIdLookupDto>(query, Params: dbValues);
        }

        private async Task<Dictionary<string, int>> GetNameIdLookupAsync(string query)
        {
            var result = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var rows = await DBHelper.GetListAsync<NameIdLookupDto>(query);

            foreach (var row in rows ?? new List<NameIdLookupDto>())
            {
                string name = (row.Name ?? string.Empty).Trim();
                if (name.Length > 0 && !result.ContainsKey(name)) result[name] = row.ID;
            }

            return result;
        }

        private async Task<Dictionary<int, HashSet<string>>> GetVariationNameLookupAsync()
        {
            var result = new Dictionary<int, HashSet<string>>();
            var rows = await DBHelper.GetListAsync<VariationNameLookupDto>("SELECT ItemID, VariationName FROM Inventory.InventoryItemVariations");

            foreach (var row in rows ?? new List<VariationNameLookupDto>())
            {
                if (!result.ContainsKey(row.ItemID))
                {
                    result[row.ItemID] = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                }

                string variationName = (row.VariationName ?? string.Empty).Trim();
                if (variationName.Length > 0) result[row.ItemID].Add(variationName);
            }

            return result;
        }

        private async Task<int> CreateManufacturerAsync(string name, string username)
        {
            var entity = new Manufacturer
            {
                Name = name,
                Phone = string.Empty,
                Email = string.Empty,
                City = string.Empty,
                CreatedBy = username,
                CreatedOn = DateTime.Now,
                IsDeleted = false
            };

            int id = 0;
            int.TryParse(await DBHelper.InsertAndGetIdAsync(entity), out id);
            return id;
        }

        private async Task<int> CreateSupplierAsync(string name, string username)
        {
            var entity = new Supplier
            {
                Name = name,
                Phone = string.Empty,
                Email = string.Empty,
                City = string.Empty,
                CreatedBy = username,
                CreatedOn = DateTime.Now,
                IsDeleted = false
            };

            int id = 0;
            int.TryParse(await DBHelper.InsertAndGetIdAsync(entity), out id);
            return id;
        }

        private static void AddImportError(ImportItemsOutputDto output, string rowLabel, string message)
        {
            if (output.Errors.Count >= 50) return;
            output.Errors.Add("Row " + rowLabel + ": " + message);
        }

        private static bool TryParseImportDate(string value, out DateTime result)
        {
            result = default(DateTime);
            if (string.IsNullOrWhiteSpace(value)) return false;

            string text = value.Trim();

            if (text.Length > 10 && text.IndexOf(' ') > 0)
            {
                text = text.Substring(0, text.IndexOf(' '));
            }

            string[] formats = { "dd-MMM-yyyy", "dd-MM-yyyy", "dd/MM/yyyy", "yyyy-MM-dd", "yyyy/MM/dd", "MM/dd/yyyy", "d/M/yyyy", "dd-MMM-yy" };

            if (DateTime.TryParseExact(text, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out result)) return true;
            if (DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.None, out result)) return true;
            if (DateTime.TryParse(text, CultureInfo.GetCultureInfo("en-GB"), DateTimeStyles.None, out result)) return true;

            return false;
        }

        private static bool TryParseImportDecimal(string value, out decimal result)
        {
            result = 0;
            if (string.IsNullOrWhiteSpace(value)) return false;

            string text = value.Trim().Replace(",", string.Empty).Replace("Rs.", string.Empty).Replace("Rs", string.Empty).Trim();
            if (text.Length == 0) return false;

            return decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out result);
        }

        #endregion
    }
}
using CoreMedixBeta.DbModels;
using CoreMedixBeta.Models.InventoryItems;
using CoreMedixBeta.Models.InventorySettings;
using CoreMedixBeta.Models.InventorySettings.DTOs;
using InstaCareEMR.Core.DBHelpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace CoreMedixBeta.Repositories
{
    public class ItemAttributesRepository
    {
        public async Task<int> SaveInvoiceSettingAsync(InvoiceSetting invoiceSetting)
        {
            try
            {
                if (invoiceSetting.ID == 0)
                {
                    return int.Parse(await DBHelper.InsertAndGetIdAsync(invoiceSetting));
                }
                else
                {
                    bool IsUpdated = await DBHelper.UpdateAsync(invoiceSetting, "WHERE ID = @ID", new Dictionary<string, string>
                    {
                        {"@ID", invoiceSetting.ID.ToString() }
                    });
                    if (IsUpdated) return invoiceSetting.ID;
                    return 0;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return 0;
            }
        }

        public async Task<int> SaveAttributeAsync(ItemAttribute attribute)
        {
            try
            {
                if (attribute.ID == 0)
                {
                    attribute.TimeStamp = ServerConfig.DateTimeNow;
                    return int.Parse(await DBHelper.InsertAndGetIdAsync(attribute));
                }
                else
                {
                    bool IsUpdated = await DBHelper.UpdateAsync(attribute, "WHERE ID = @ID", new Dictionary<string, string>
                    {
                        {"@ID", attribute.ID.ToString() }
                    });
                    if (IsUpdated) return attribute.ID;
                    return 0;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return 0;
            }
        }

        public async Task<InvoiceSetting> GetInvoiceSettingAsync(GetInvoiceSettingInputDto input)
        {
            try
            {
                Dictionary<string, string> dbValues = new Dictionary<string, string>
                {
                };
                string query = @" SELECT TOP 1 ID, InvoicePrefix,Currency
                               FROM Invoice.InvoiceSettings ";

                return await DBHelper.GetAsync<InvoiceSetting>(query, Params: dbValues);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return new InvoiceSetting();
            }
        }

        public async Task<List<GetAllAttributesOutputDto>> GetAllAttributesAsync(GetAllAttributesInputDto input)
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

        public async Task<bool> DeleteAttributeAsync(DeleteAttributeInputDto input)
        {
            try
            {
                Dictionary<string, string> dbValues = new Dictionary<string, string>
                {
                    {"@ID", input.ID.ToString() },
                };
                string query = @"DELETE FROM Inventory.ItemAttributes WHERE ID = @ID ";
                return await DBHelper.ExecuteQueryAsync(query, dbValues);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return false;
            }
        }

        public async Task<string> GetInvoicePrefixAsync(GetInvoiceSettingInputDto input)
        {
            try
            {
                Dictionary<string, string> dbValues = new Dictionary<string, string>
                {
                };

                string query = @"SELECT TOP 1 InvoicePrefix FROM Invoice.InvoiceSettings";
                var prefix = await DBHelper.GetSingleColumnAsync(query, dbValues);

                if (string.IsNullOrWhiteSpace(prefix))
                {
                    prefix = "INV";
                }

                string invoiceNumberQuery = @"SELECT TOP 1 InvoiceNo
                              FROM Pharmacy.SaleInvoices WITH (UPDLOCK, HOLDLOCK)
                              WHERE MasterUsername = @MasterUsername
                              ORDER BY ID DESC ";

                var invoiceNumber = await DBHelper.GetSingleColumnAsync(invoiceNumberQuery, dbValues);

                if (string.IsNullOrWhiteSpace(invoiceNumber))
                {
                    return $"{prefix}/1";
                }

                var numberPart = invoiceNumber.Replace(prefix, "").TrimStart('/');
                if (int.TryParse(numberPart, out int currentNumber))
                {
                    var nextNumber = currentNumber + 1;

                    return $"{prefix}/{nextNumber}";
                }
                return $"{prefix}/1";
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return string.Empty;
            }
        }

        public async Task<PrinterPrefernce> GetPreferenceAsync(GetPreferenceInputDto input)
        {
            try
            {
                Dictionary<string, string> dbValues = new Dictionary<string, string>
                {
                };

                string query = @"SELECT TOP 1 * FROM Printer.PrinterPrefernces";

                PrinterPrefernce data = await DBHelper.GetAsync<PrinterPrefernce>(query, Params: dbValues);

                if (data is null || data.ID is 0)
                {
                    return await SaveAndGetPreferenceAsync(new PrinterPrefernce
                    {
                        Type = "THERMAL"
                    });
                }

                return data;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return new PrinterPrefernce();
            }
        }

        public async Task<PrinterPrefernce> SaveAndGetPreferenceAsync(PrinterPrefernce input)
        {
            try
            {
                Dictionary<string, string> dbValues = new Dictionary<string, string>
                {
                };

                if (input.ID == 0)
                {
                    string checkQuery = @"SELECT TOP 1 * FROM Printer.PrinterPrefernces";

                    var existingPreference = await DBHelper.GetAsync<PrinterPrefernce>(checkQuery, Params: dbValues);

                    if (existingPreference?.ID > 0)
                    {
                        input.ID = existingPreference.ID;
                    }
                    else
                    {
                        input.ID = int.Parse(await DBHelper.InsertAndGetIdAsync(input));
                        return input;
                    }
                }

                dbValues.Add("@ID", input.ID.ToString());
                input.LastModifiedOn = DateTime.Now;
                await DBHelper.UpdateAsync(input, "WHERE ID = @ID", dbValues);

                return input;
            }
            catch
            {
                return new PrinterPrefernce();
            }
        }

        public async Task<string> GetCurrencyAsync(GetPreferenceInputDto input)
        {
            try
            {
                Dictionary<string, string> dbValues = new Dictionary<string, string>
                {
                };

                string query = @"SELECT TOP 1 Currency FROM Invoice.InvoiceSettings";
                return await DBHelper.GetSingleColumnAsync(query, dbValues);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return string.Empty;
            }
        }
    }
}
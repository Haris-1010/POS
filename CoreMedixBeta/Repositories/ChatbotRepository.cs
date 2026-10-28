using InstaCareEMR.Core.DBHelpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static System.Web.Razor.Parser.SyntaxConstants;

namespace CoreMedixBeta.Repositories
{
    public class ChatbotRepository
    {
        // Detected language of the current query: "en" = English, "ur" = Urdu (Roman/Script)
        private string _lang = "en";

        // Returns the string matching the detected language
        private string L(string urdu, string english)
        {
            return _lang == "ur" ? urdu : english;
        }

        private string DetectLanguage(string prompt)
        {
            if (string.IsNullOrWhiteSpace(prompt))
                return "en";

            var lower = prompt.ToLower().Trim();

            // Urdu script (اردو) detect
            if (Regex.IsMatch(prompt, "[\u0600-\u06FF]"))
                return "ur";

            // Roman Urdu markers - agar koi bhi mila to reply Urdu mein
            string[] urduWords = {
                "kya","kia","kiya","kiye","kaise","kese","kaisa","kaisi","kitna","kitni","kitnay","kitne",
                "hai","hain","hy","hun","houn","hoga","hogi","hota","hoti",
                "mujhe","mujhy","mujhko","aap","apni","apna","apnay","tum","mera","meri","mere","meray",
                "hamara","humari","batao","batayein","bataen","btao","btaen","btana","dikhao",
                "chahiye","chahyay","chahie","chahta","chahti","nahi","nhi","nahin",
                "karna","karni","kro","karein","kar","krna","raha","rahe","rahi","rhi","gaya","gayi","gyi",
                "kis","konsa","konsi","kaun","kaunsa","zara","plz","plzz","shukriya","shukria","shukar",
                "salam","assalam","assalamualaikum","allah hafiz","khuda hafiz","ji",
                "mariz","bimar","bimari","dawa","dawai","dawayein","bikri","khareed","khareedi","khareedna",
                "faraham","bacha","maal","tafseel","tafseelan","milta","milti","mili","mily","nahi mila",
                "acha","achha","theek","thek","thik","yaar","bhai","abhi","phir","pahir","aur",
                "karo","krdo","dena","dijiyega","lena","hogaya","hogya","kyun","kyu","kio","kyunki",
                "madad","ka","ki","ke","ko","se","wala","walay","liye","bikhre"
            };

            int urScore = urduWords.Count(w => IsWordPresent(lower, w));

            if (urScore > 0)
                return "ur";

            return "en";
        }

        private bool IsWordPresent(string text, string word)
        {
            if (word.Contains(" "))
                return text.Contains(word);

            return Regex.IsMatch(text, @"\b" + Regex.Escape(word) + @"\b");
        }

        // AI integration method
        public async Task<string> GetAIResponse(string prompt)
        {
            try
            {
                // Detect the language the user is asking in
                _lang = DetectLanguage(prompt);

                // Log the query
                await LogQuery(prompt, "Processing...");

                // Check query type and provide appropriate response
                var response = await ProcessSmartQuery(prompt);

                // Log the response
                await LogQuery(prompt, response, true);

                return response;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Chatbot Error: {ex.Message}");
                return L("Mujhe technical issue aa raha hai. Baad mein try karein ya staff se contact karein.",
                         "I'm facing a technical issue. Please try again later or contact the staff.");
            }
        }

        private async Task<string> ProcessSmartQuery(string prompt)
        {
            var lowerPrompt = prompt.ToLower();

            // 🔹 DIRECT MEDICINE NAME QUERY - PEHLE CHECK KAREIN
            var medicineName = await ExtractMedicineName(lowerPrompt);
            if (!string.IsNullOrEmpty(medicineName))
            {
                if (await IsOnlyMedicineQuery(lowerPrompt, medicineName))
                {
                    return await GetMedicineStock(medicineName);
                }
            }

            // 🔹 COMPREHENSIVE STOCK/INVENTORY QUERIES
            if (ContainsAny(lowerPrompt, "stock", "inventory", "available", "quantity", "kitna", "bacha", "maal", "item", "product"))
            {
                return await HandleStockQuery(lowerPrompt);
            }

            // 🔹 DETAILED CUSTOMER QUERIES
            if (ContainsAny(lowerPrompt, "patient", "customer", "mariz", "bimar", "history", "report", "visit", "mr no", "mrn"))
            {
                return await HandlePatientQuery(lowerPrompt);
            }

            // 🔹 COMPLETE SALES/INVOICE QUERIES
            if (ContainsAny(lowerPrompt, "sale", "invoice", "bill", "bikri", "selling", "revenue", "income"))
            {
                return await HandleSalesQuery(lowerPrompt);
            }

            // 🔹 PURCHASE ORDER QUERIES
            if (ContainsAny(lowerPrompt, "purchase", "khareed", "order", "po", "received", "supply"))
            {
                return await HandlePurchaseQuery(lowerPrompt);
            }

            // 🔹 SUPPLIER QUERIES
            if (ContainsAny(lowerPrompt, "supplier", "vendor", "dealer", "faraham karnay wala"))
            {
                return await HandleSupplierQuery(lowerPrompt);
            }

            // 🔹 MANUFACTURER & BRAND QUERIES
            if (ContainsAny(lowerPrompt, "manufacturer", "company", "bananay wala", "brand", "brands"))
            {
                return await HandleManufacturerQuery(lowerPrompt);
            }

            // 🔹 EXPIRY RELATED QUERIES
            if (ContainsAny(lowerPrompt, "expiry", "expire", "khatam", "date", "expired"))
            {
                return await HandleExpiryQuery(lowerPrompt);
            }

            // 🔹 REPORT QUERIES
            if (ContainsAny(lowerPrompt, "report", "summary", "detail", "details", "tafseel"))
            {
                return await HandleReportQuery(lowerPrompt);
            }

            // 🔹 GENERAL QUERIES
            if (ContainsAny(lowerPrompt, "hello", "hi", "hey", "salam", "assalam"))
            {
                return GetGreetingResponse();
            }

            if (ContainsAny(lowerPrompt, "thank", "shukriya", "thanks"))
            {
                return L("Aapka shukriya! 😊 Koi aur help chahiye?",
                         "Thank you! 😊 Do you need any more help?");
            }

            if (ContainsAny(lowerPrompt, "bye", "goodbye", "khuda hafiz", "allah hafiz"))
            {
                return L("Allah Hafiz! 👍 Koi aur sawal ho to poochhein.",
                         "Goodbye! 👍 Ask if you have any other question.");
            }

            // Default AI response
            return await GetDefaultAIResponse(prompt);
        }

        #region 🔹 COMPREHENSIVE STOCK/INVENTORY HANDLERS

        private async Task<string> HandleStockQuery(string prompt)
        {
            // Specific medicine stock
            var medicineName = await ExtractMedicineName(prompt);
            if (!string.IsNullOrEmpty(medicineName))
            {
                return await GetMedicineStock(medicineName);
            }

            // Total stock summary
            if (ContainsAny(prompt, "total", "overall", "complete", "summary"))
            {
                return await GetCompleteStockSummary();
            }

            // Low stock items
            if (ContainsAny(prompt, "low", "kam", "khatam", "finish", "khatam honay wala"))
            {
                return await GetLowStockItems();
            }

            // Out of stock items
            if (ContainsAny(prompt, "out of stock", "khatam", "nahi hai", "not available", "out"))
            {
                return await GetOutOfStockItems();
            }

            // Product details
            if (ContainsAny(prompt, "detail", "details", "information", "info", "tafseel"))
            {
                return await GetProductDetails(prompt);
            }

            // Default stock summary
            return await GetCompleteStockSummary();
        }

        private async Task<string> GetMedicineStock(string medicineName)
        {
            try
            {
                string query = @"
                    SELECT
                        i.Name as ItemName,
                        v.OpeningStock as CurrentStock,
                        v.ReorderPoint as MinimumStock,
                        v.SellingPrice as UnitPrice,
                        m.Name as ManufacturerName
                    FROM Inventory.InventoryItems i
                    INNER JOIN Inventory.InventoryItemVariations v ON i.ID = v.ItemID
                    INNER JOIN Manufacturer.Manufacturers m ON i.ManufacturerID = m.ID
                    WHERE i.Name LIKE @MedicineName
                    AND i.IsDeleted = 0";

                var parameters = new Dictionary<string, string>
                {
                    { "@MedicineName", $"%{medicineName}%" }
                };

                var results = await DBHelper.GetListAsync<dynamic>(
                    query: query,
                    whereClause: null,
                    Params: parameters
                );

                if (results != null && results.Any())
                {
                    var response = L($"💊 **{medicineName} ka Stock:**\n\n",
                                     $"💊 **{medicineName} Stock Details:**\n\n");

                    foreach (var row in results)
                    {
                        string itemName = row.ItemName?.ToString() ?? "";
                        int currentStock = Convert.ToInt32(row.CurrentStock);
                        int minimumStock = Convert.ToInt32(row.MinimumStock);
                        decimal unitPrice = Convert.ToDecimal(row.UnitPrice);
                        string manufacturer = row.ManufacturerName?.ToString() ?? "";

                        string status = currentStock == 0 ? L("❌ Stock Khatam", "❌ Out of Stock") :
                                       currentStock <= minimumStock ? L("⚠️ Stock Kam", "⚠️ Low Stock") : L("✅ Stock Mojood", "✅ In Stock");

                        response += L($"**{itemName}**\n" +
                                      $"• Stock: {currentStock}\n" +
                                      $"• Halat: {status}\n" +
                                      $"• Qeemat: Rs. {unitPrice}\n" +
                                      $"• Company: {manufacturer}\n\n",
                                      $"**{itemName}**\n" +
                                      $"• Stock: {currentStock}\n" +
                                      $"• Status: {status}\n" +
                                      $"• Price: Rs. {unitPrice}\n" +
                                      $"• Company: {manufacturer}\n\n");
                    }

                    return response;
                }
                else
                {
                    // Agar exact match nahi mila to similar products suggest karein
                    var similarMedicines = await FindSimilarMedicines(medicineName);
                    if (similarMedicines.Any())
                    {
                        return L($"'{medicineName}' nahi mila. Kya aap in products ki baat kar rahe hain?\n" +
                                 string.Join(", ", similarMedicines.Take(5)) +
                                 "\n\nInme se kisi ka naam likhein.",
                                 $"'{medicineName}' was not found. Did you mean one of these products?\n" +
                                 string.Join(", ", similarMedicines.Take(5)) +
                                 "\n\nType the name of any one of them.");
                    }

                    return L($"'{medicineName}' product nahi mila. Spelling check karein ya staff se poochein.",
                             $"Product '{medicineName}' was not found. Please check the spelling or ask the staff.");
                }
            }
            catch (Exception ex)
            {
                return L($"Stock check mein issue aa raha hai: {ex.Message}",
                         $"There is an issue while checking the stock: {ex.Message}");
            }
        }

        private async Task<string> GetCompleteStockSummary()
        {
            try
            {
                string query = @"
                    SELECT
                        COUNT(DISTINCT i.ID) as TotalProducts,
                        COUNT(v.ID) as TotalVariations,
                        SUM(v.OpeningStock) as TotalStockQuantity,
                        SUM(v.OpeningStock * v.CostPrice) as TotalStockValue,
                        SUM(CASE WHEN v.OpeningStock <= v.ReorderPoint AND v.OpeningStock > 0 THEN 1 ELSE 0 END) as LowStockItems,
                        SUM(CASE WHEN v.OpeningStock = 0 THEN 1 ELSE 0 END) as OutOfStockItems,
                        AVG(v.SellingPrice) as AveragePrice
                    FROM Inventory.InventoryItems i
                    INNER JOIN Inventory.InventoryItemVariations v ON i.ID = v.ItemID
                    WHERE i.IsDeleted = 0";

                var result = await DBHelper.GetAsync<dynamic>(query: query);

                if (result != null)
                {
                    int totalProducts = Convert.ToInt32(result.TotalProducts);
                    int totalVariations = Convert.ToInt32(result.TotalVariations);
                    int totalStock = Convert.ToInt32(result.TotalStockQuantity);
                    decimal totalValue = result.TotalStockValue != null ? Convert.ToDecimal(result.TotalStockValue) : 0;
                    int lowStock = Convert.ToInt32(result.LowStockItems);
                    int outOfStock = Convert.ToInt32(result.OutOfStockItems);
                    decimal avgPrice = result.AveragePrice != null ? Convert.ToDecimal(result.AveragePrice) : 0;

                    return L($"📊 **Mukammal Stock Summary:**\n\n" +
                             $"• Total Products: {totalProducts}\n" +
                             $"• Total Variations: {totalVariations}\n" +
                             $"• Total Stock Quantity: {totalStock:N0}\n" +
                             $"• Total Stock Value: Rs. {totalValue:N0}\n" +
                             $"• Low Stock Items: {lowStock}\n" +
                             $"• Out of Stock Items: {outOfStock}\n" +
                             $"• Average Price: Rs. {avgPrice:N2}\n\n" +
                             $"Detailed reports ke liye 'low stock report' ya 'out of stock report' likhein.",
                             $"📊 **Complete Stock Summary:**\n\n" +
                             $"• Total Products: {totalProducts}\n" +
                             $"• Total Variations: {totalVariations}\n" +
                             $"• Total Stock Quantity: {totalStock:N0}\n" +
                             $"• Total Stock Value: Rs. {totalValue:N0}\n" +
                             $"• Low Stock Items: {lowStock}\n" +
                             $"• Out of Stock Items: {outOfStock}\n" +
                             $"• Average Price: Rs. {avgPrice:N2}\n\n" +
                             $"For detailed reports type 'low stock report' or 'out of stock report'.");
                }

                return L("Stock summary nahi mil saka.", "Stock summary could not be found.");
            }
            catch (Exception ex)
            {
                return L($"Summary check mein issue: {ex.Message}",
                         $"Issue while checking the summary: {ex.Message}");
            }
        }

        private async Task<string> GetProductDetails(string prompt)
        {
            try
            {
                string query = @"
                    SELECT TOP 10
                        i.Name as ProductName,
                        v.VariationName,
                        v.OpeningStock as CurrentStock,
                        v.ReorderPoint as MinStock,
                        v.SellingPrice as Price,
                        v.CostPrice,
                        m.Name as Manufacturer,
                        v.ExpiryDate
                    FROM Inventory.InventoryItems i
                    INNER JOIN Inventory.InventoryItemVariations v ON i.ID = v.ItemID
                    INNER JOIN Manufacturer.Manufacturers m ON i.ManufacturerID = m.ID
                    WHERE i.IsDeleted = 0
                    ORDER BY i.Name";

                var results = await DBHelper.GetListAsync<dynamic>(query: query);

                if (results != null && results.Any())
                {
                    var response = L("📦 **Product ki Tafseel (Top 10):**\n\n",
                                     "📦 **Product Details (Top 10):**\n\n");

                    foreach (var row in results)
                    {
                        string productName = row.ProductName?.ToString() ?? "";
                        string variation = row.VariationName?.ToString() ?? "";
                        int stock = Convert.ToInt32(row.CurrentStock);
                        decimal price = Convert.ToDecimal(row.Price);
                        string manufacturer = row.Manufacturer?.ToString() ?? "";
                        DateTime expiry = row.ExpiryDate != null ? Convert.ToDateTime(row.ExpiryDate) : DateTime.MinValue;

                        response += L($"**{productName}** ({variation})\n" +
                                      $"• Stock: {stock} | Qeemat: Rs. {price}\n" +
                                      $"• Company: {manufacturer}\n" +
                                      $"• Expiry: {(expiry > DateTime.MinValue ? expiry.ToString("MMM yyyy") : "N/A")}\n\n",
                                      $"**{productName}** ({variation})\n" +
                                      $"• Stock: {stock} | Price: Rs. {price}\n" +
                                      $"• Manufacturer: {manufacturer}\n" +
                                      $"• Expiry: {(expiry > DateTime.MinValue ? expiry.ToString("MMM yyyy") : "N/A")}\n\n");
                    }

                    return response;
                }

                return L("Product details nahi mil sakin.", "Product details could not be found.");
            }
            catch (Exception ex)
            {
                return L($"Product details check mein issue: {ex.Message}",
                         $"Issue while checking product details: {ex.Message}");
            }
        }

        private async Task<string> GetLowStockItems()
        {
            try
            {
                string query = @"
                    SELECT
                        i.Name as ItemName,
                        v.OpeningStock as CurrentStock,
                        v.ReorderPoint as MinimumStock,
                        v.SellingPrice as UnitPrice
                    FROM Inventory.InventoryItems i
                    INNER JOIN Inventory.InventoryItemVariations v ON i.ID = v.ItemID
                    WHERE v.OpeningStock <= v.ReorderPoint
                    AND v.OpeningStock > 0
                    AND i.IsDeleted = 0
                    ORDER BY v.OpeningStock ASC";

                var results = await DBHelper.GetListAsync<dynamic>(query: query);

                if (results != null && results.Any())
                {
                    var response = L("⚠️ **Kam Stock walay Items:**\n\n",
                                     "⚠️ **Low Stock Items:**\n\n");

                    foreach (var row in results)
                    {
                        string itemName = row.ItemName?.ToString() ?? "";
                        int currentStock = Convert.ToInt32(row.CurrentStock);
                        int minimumStock = Convert.ToInt32(row.MinimumStock);

                        response += L($"**{itemName}**\n" +
                                      $"• Mojooda: {currentStock}\n" +
                                      $"• Minimum: {minimumStock}\n" +
                                      $"• Kami: {minimumStock - currentStock}\n\n",
                                      $"**{itemName}**\n" +
                                      $"• Current: {currentStock}\n" +
                                      $"• Minimum: {minimumStock}\n" +
                                      $"• Shortage: {minimumStock - currentStock}\n\n");
                    }

                    return response;
                }
                else
                {
                    return L("Alhamdulillah! ✅ Koi low stock item nahi hai.",
                             "Alhamdulillah! ✅ There is no low stock item.");
                }
            }
            catch (Exception ex)
            {
                return L($"Low stock check mein issue: {ex.Message}",
                         $"Issue while checking low stock: {ex.Message}");
            }
        }

        private async Task<string> GetOutOfStockItems()
        {
            try
            {
                string query = @"
                    SELECT
                        i.Name as ItemName,
                        v.ReorderPoint as MinimumStock,
                        v.SellingPrice as UnitPrice
                    FROM Inventory.InventoryItems i
                    INNER JOIN Inventory.InventoryItemVariations v ON i.ID = v.ItemID
                    WHERE v.OpeningStock = 0
                    AND i.IsDeleted = 0
                    ORDER BY i.Name";

                var results = await DBHelper.GetListAsync<dynamic>(query: query);

                if (results != null && results.Any())
                {
                    var response = L("❌ **Stock Khatam Items:**\n\n",
                                     "❌ **Out of Stock Items:**\n\n");

                    foreach (var row in results)
                    {
                        string itemName = row.ItemName?.ToString() ?? "";
                        decimal unitPrice = Convert.ToDecimal(row.UnitPrice);

                        response += $"• {itemName} - Rs. {unitPrice}\n";
                    }

                    response += L($"\nTotal {results.Count} items stock se khatam hain.",
                                  $"\nTotal {results.Count} items are out of stock.");
                    return response;
                }
                else
                {
                    return L("Alhamdulillah! ✅ Koi item out of stock nahi hai.",
                             "Alhamdulillah! ✅ No item is out of stock.");
                }
            }
            catch (Exception ex)
            {
                return L($"Out of stock check mein issue: {ex.Message}",
                         $"Issue while checking out of stock items: {ex.Message}");
            }
        }

        #endregion 🔹 COMPREHENSIVE STOCK/INVENTORY HANDLERS

        #region 🔹 DETAILED PATIENT HANDLERS

        private async Task<string> HandlePatientQuery(string prompt)
        {
            // Extract patient name or ID
            var patientInfo = ExtractPatientInfo(prompt);

            if (!string.IsNullOrEmpty(patientInfo))
            {
                return await GetPatientDetails(patientInfo);
            }

            // Total patients count
            if (ContainsAny(prompt, "total", "kitnay", "count", "number", "overall"))
            {
                return await GetTotalPatients();
            }

            // Patient invoices
            if (ContainsAny(prompt, "invoice", "bill", "sale", "purchase", "history"))
            {
                return await GetPatientInvoices(prompt);
            }

            // Recent patients
            if (ContainsAny(prompt, "recent", "naye", "new", "latest"))
            {
                return await GetRecentPatients();
            }

            return await GetTotalPatients();
        }

        private async Task<string> GetPatientDetails(string patientIdentifier)
        {
            try
            {
                string query = @"
                    SELECT
                        ID,
                        Name,
                        Phone,
                        MR_No,
                        City,
                        DateOfBirth,
                        Gender,
                        CreatedOn as LastVisitDate
                    FROM Patient.Patients
                    WHERE (Name LIKE @PatientName OR MR_No = @PatientID)
                    AND IsDeleted = 0";

                var parameters = new Dictionary<string, string>
                {
                    { "@PatientName", $"%{patientIdentifier}%" },
                    { "@PatientID", patientIdentifier }
                };

                var results = await DBHelper.GetListAsync<dynamic>(
                    query: query,
                    whereClause: null,
                    Params: parameters
                );

                if (results != null && results.Any())
                {
                    var row = results.First();
                    string response = L("👤 **Mariz ki Maloomat:**\n\n",
                                        "👤 **Patient Information:**\n\n");

                    response += L($"• **Naam:** {row.Name}\n" +
                                  $"• **MR No:** {row.MR_No}\n" +
                                  $"• **Phone:** {row.Phone}\n" +
                                  $"• **Shehar:** {row.City}\n" +
                                  $"• **Date of Birth:** {(row.DateOfBirth != null ? Convert.ToDateTime(row.DateOfBirth).ToString("dd-MMM-yyyy") : "N/A")}\n" +
                                  $"• **Gender:** {row.Gender}\n" +
                                  $"• **Aakhri Visit:** {Convert.ToDateTime(row.LastVisitDate):dd-MMM-yyyy}\n\n" +
                                  $"Detailed medical history ke liye records system check karein.",
                                  $"• **Name:** {row.Name}\n" +
                                  $"• **MR No:** {row.MR_No}\n" +
                                  $"• **Phone:** {row.Phone}\n" +
                                  $"• **City:** {row.City}\n" +
                                  $"• **Date of Birth:** {(row.DateOfBirth != null ? Convert.ToDateTime(row.DateOfBirth).ToString("dd-MMM-yyyy") : "N/A")}\n" +
                                  $"• **Gender:** {row.Gender}\n" +
                                  $"• **Last Visit:** {Convert.ToDateTime(row.LastVisitDate):dd-MMM-yyyy}\n\n" +
                                  $"For detailed medical history, check the records system.");

                    return response;
                }
                else
                {
                    return L($"Customer '{patientIdentifier}' nahi mila. Details check karein.",
                             $"Customer '{patientIdentifier}' was not found. Please check the details.");
                }
            }
            catch (Exception ex)
            {
                return L($"Customer info mein issue: {ex.Message}",
                         $"Issue while fetching customer info: {ex.Message}");
            }
        }

        private async Task<string> GetTotalPatients()
        {
            try
            {
                string query = @"
                    SELECT
                        COUNT(*) as TotalPatients,
                        COUNT(CASE WHEN Gender = 'Male' THEN 1 END) as MalePatients,
                        COUNT(CASE WHEN Gender = 'Female' THEN 1 END) as FemalePatients,
                        MAX(CreatedOn) as LastRegistration
                    FROM Patient.Patients
                    WHERE IsDeleted = 0";

                var result = await DBHelper.GetAsync<dynamic>(query: query);

                if (result != null)
                {
                    int total = Convert.ToInt32(result.TotalPatients);
                    int male = Convert.ToInt32(result.MalePatients);
                    int female = Convert.ToInt32(result.FemalePatients);
                    DateTime lastReg = result.LastRegistration != null ? Convert.ToDateTime(result.LastRegistration) : DateTime.Now;

                    return L($"👥 **Kul Customers ki Tadaad:**\n\n" +
                             $"• Kul Customers: {total}\n" +
                             $"• Male: {male}\n" +
                             $"• Female: {female}\n" +
                             $"• Aakhri Registration: {lastReg:dd-MMM-yyyy}\n\n" +
                             $"Specific customer ke liye 'customer [name]' likhein.",
                             $"👥 **Total Customers Summary:**\n\n" +
                             $"• Total Customers: {total}\n" +
                             $"• Male Customers: {male}\n" +
                             $"• Female Customers: {female}\n" +
                             $"• Last Registration: {lastReg:dd-MMM-yyyy}\n\n" +
                             $"For a specific customer type 'customer [name]'.");
                }

                return L("Customer count nahi mil saka.", "Customer count could not be found.");
            }
            catch (Exception ex)
            {
                return L($"Customer count check mein issue: {ex.Message}",
                         $"Issue while checking customer count: {ex.Message}");
            }
        }

        private async Task<string> GetPatientInvoices(string prompt)
        {
            try
            {
                string patientName = ExtractPatientInfo(prompt);

                string query = @"
                    SELECT TOP 5
                        s.InvoiceNo,
                        s.InvoiceDate,
                        f.SubtotalAfterTax as TotalAmount,
                        f.PaidAmount,
                        f.RemainingAmount
                    FROM Sale.SaleInvoices s
                    INNER JOIN Sale.SaleInvoiceFinancialDetails f ON s.ID = f.InvoiceID
                    WHERE s.CustomerID LIKE @PatientName
                    AND s.IsDeleted = 0
                    ORDER BY s.InvoiceDate DESC";

                var parameters = new Dictionary<string, string>
                {
                    { "@PatientName", $"%{patientName}%" }
                };

                var results = await DBHelper.GetListAsync<dynamic>(query: query, Params: parameters);

                if (results != null && results.Any())
                {
                    var response = L($"🧾 **{patientName} - Haal ki Invoices:**\n\n",
                                     $"🧾 **{patientName} - Recent Invoices:**\n\n");

                    foreach (var row in results)
                    {
                        string invoiceNo = row.InvoiceNo?.ToString() ?? "";
                        DateTime invoiceDate = Convert.ToDateTime(row.InvoiceDate);
                        decimal total = Convert.ToDecimal(row.TotalAmount);
                        decimal paid = Convert.ToDecimal(row.PaidAmount);
                        decimal remaining = Convert.ToDecimal(row.RemainingAmount);

                        response += L($"**Invoice {invoiceNo}**\n" +
                                      $"• Tareekh: {invoiceDate:dd-MMM-yyyy}\n" +
                                      $"• Kul: Rs. {total:N0}\n" +
                                      $"• Ada Shuda: Rs. {paid:N0}\n" +
                                      $"• Baqaya: Rs. {remaining:N0}\n\n",
                                      $"**Invoice {invoiceNo}**\n" +
                                      $"• Date: {invoiceDate:dd-MMM-yyyy}\n" +
                                      $"• Total: Rs. {total:N0}\n" +
                                      $"• Paid: Rs. {paid:N0}\n" +
                                      $"• Due: Rs. {remaining:N0}\n\n");
                    }

                    return response;
                }
                else
                {
                    return L($"'{patientName}' ke koi invoices nahi milay.",
                             $"No invoices found for '{patientName}'.");
                }
            }
            catch (Exception ex)
            {
                return L($"Customer invoices check mein issue: {ex.Message}",
                         $"Issue while checking customer invoices: {ex.Message}");
            }
        }

        private async Task<string> GetRecentPatients()
        {
            try
            {
                string query = @"
                    SELECT TOP 5
                        Name,
                        MR_No,
                        Phone,
                        CreatedOn as LastVisitDate
                    FROM Patient.Patients
                    WHERE IsDeleted = 0
                    ORDER BY CreatedOn DESC";

                var results = await DBHelper.GetListAsync<dynamic>(query: query);

                if (results != null && results.Any())
                {
                    var response = L("🆕 **Haal ke Customers:**\n\n",
                                     "🆕 **Recent Customers:**\n\n");

                    foreach (var row in results)
                    {
                        response += $"• {row.Name} ({row.MR_No}) - {Convert.ToDateTime(row.LastVisitDate):dd-MMM-yyyy}\n";
                    }

                    return response;
                }
                else
                {
                    return L("Koi recent customer nahi mila.", "No recent customer found.");
                }
            }
            catch (Exception ex)
            {
                return L($"Recent patients check mein issue: {ex.Message}",
                         $"Issue while checking recent patients: {ex.Message}");
            }
        }

        #endregion 🔹 DETAILED PATIENT HANDLERS

        #region 🔹 COMPLETE SALES HANDLERS

        private async Task<string> HandleSalesQuery(string prompt)
        {
            // Today's sales
            if (ContainsAny(prompt, "today", "aaj", "aj", "din"))
            {
                return await GetTodaysSales();
            }

            // Sales summary
            if (ContainsAny(prompt, "total", "overall", "summary"))
            {
                return await GetCompleteSalesSummary();
            }

            return await GetCompleteSalesSummary();
        }

        private async Task<string> GetTodaysSales()
        {
            try
            {
                string query = @"
                    SELECT
                        COUNT(*) as TotalInvoices,
                        SUM(f.SubtotalAfterTax) as TotalSales,
                        SUM(f.PaidAmount) as TotalPaid
                    FROM Sale.SaleInvoices s
                    INNER JOIN Sale.SaleInvoiceFinancialDetails f ON s.ID = f.InvoiceID
                    WHERE CAST(s.InvoiceDate AS DATE) = CAST(GETDATE() AS DATE)
                    AND s.IsDeleted = 0";

                var result = await DBHelper.GetAsync<dynamic>(query: query);

                if (result != null)
                {
                    int invoices = Convert.ToInt32(result.TotalInvoices);
                    decimal sales = result.TotalSales != null ? Convert.ToDecimal(result.TotalSales) : 0;
                    decimal paid = result.TotalPaid != null ? Convert.ToDecimal(result.TotalPaid) : 0;

                    return L($"💰 **Aaj ki Sales:**\n\n" +
                             $"• Kul Invoices: {invoices}\n" +
                             $"• Kul Sales: Rs. {sales:N0}\n" +
                             $"• Wasool Shuda Raqam: Rs. {paid:N0}\n" +
                             $"• Pending: Rs. {sales - paid:N0}",
                             $"💰 **Today's Sales:**\n\n" +
                             $"• Total Invoices: {invoices}\n" +
                             $"• Total Sales: Rs. {sales:N0}\n" +
                             $"• Amount Received: Rs. {paid:N0}\n" +
                             $"• Pending: Rs. {sales - paid:N0}");
                }

                return L("Aaj koi sale nahi hui hai.", "No sale has been made today.");
            }
            catch (Exception ex)
            {
                return L($"Sales check mein issue: {ex.Message}",
                         $"Issue while checking sales: {ex.Message}");
            }
        }

        private async Task<string> GetCompleteSalesSummary()
        {
            try
            {
                string query = @"
                    SELECT
                        COUNT(*) as TotalInvoices,
                        SUM(f.SubtotalAfterTax) as TotalSales,
                        SUM(f.PaidAmount) as TotalReceived,
                        SUM(f.RemainingAmount) as TotalDue,
                        AVG(f.SubtotalAfterTax) as AverageSale,
                        MAX(s.InvoiceDate) as LastSaleDate
                    FROM Sale.SaleInvoices s
                    INNER JOIN Sale.SaleInvoiceFinancialDetails f ON s.ID = f.InvoiceID
                    WHERE s.IsDeleted = 0";

                var result = await DBHelper.GetAsync<dynamic>(query: query);

                if (result != null)
                {
                    int invoices = Convert.ToInt32(result.TotalInvoices);
                    decimal sales = result.TotalSales != null ? Convert.ToDecimal(result.TotalSales) : 0;
                    decimal received = result.TotalReceived != null ? Convert.ToDecimal(result.TotalReceived) : 0;
                    decimal due = result.TotalDue != null ? Convert.ToDecimal(result.TotalDue) : 0;
                    decimal avgSale = result.AverageSale != null ? Convert.ToDecimal(result.AverageSale) : 0;
                    DateTime lastSale = result.LastSaleDate != null ? Convert.ToDateTime(result.LastSaleDate) : DateTime.Now;

                    return L($"💰 **Mukammal Sales Summary:**\n\n" +
                             $"• Kul Invoices: {invoices}\n" +
                             $"• Kul Sales: Rs. {sales:N0}\n" +
                             $"• Wasool Shuda Raqam: Rs. {received:N0}\n" +
                             $"• Outstanding Raqam: Rs. {due:N0}\n" +
                             $"• Ausat Sale: Rs. {avgSale:N0}\n" +
                             $"• Aakhri Sale: {lastSale:dd-MMM-yyyy}\n\n" +
                             $"Aaj ki sales ke liye 'today sales' likhein.",
                             $"💰 **Complete Sales Summary:**\n\n" +
                             $"• Total Invoices: {invoices}\n" +
                             $"• Total Sales: Rs. {sales:N0}\n" +
                             $"• Amount Received: Rs. {received:N0}\n" +
                             $"• Outstanding Amount: Rs. {due:N0}\n" +
                             $"• Average Sale: Rs. {avgSale:N0}\n" +
                             $"• Last Sale: {lastSale:dd-MMM-yyyy}\n\n" +
                             $"For today's sales type 'today sales'.");
                }

                return L("Sales summary nahi mil saka.", "Sales summary could not be found.");
            }
            catch (Exception ex)
            {
                return L($"Sales summary check mein issue: {ex.Message}",
                         $"Issue while checking sales summary: {ex.Message}");
            }
        }

        #endregion 🔹 COMPLETE SALES HANDLERS

        #region 🔹 PURCHASE ORDER HANDLERS

        private async Task<string> HandlePurchaseQuery(string prompt)
        {
            // Purchase orders status
            if (ContainsAny(prompt, "order", "po", "pending", "status"))
            {
                return await GetPurchaseOrdersStatus();
            }

            // Received purchases
            if (ContainsAny(prompt, "received", "delivered", "complete", "finished"))
            {
                return await GetReceivedPurchases();
            }

            return await GetPurchaseOrdersStatus();
        }

        private async Task<string> GetPurchaseOrdersStatus()
        {
            try
            {
                string query = @"
                    SELECT
                        COUNT(*) as TotalPOs,
                        COUNT(CASE WHEN Status = 'Pending' THEN 1 END) as PendingPOs, 
                        COUNT(CASE WHEN IsPurchaseReceived = 1 THEN 1 END) as ReceivedPOs,
                        SUM(SubTotal) as TotalPOValue
                    FROM Purchase.PurchaseOrders
                    WHERE IsDeleted = 0";

                var result = await DBHelper.GetAsync<dynamic>(query: query);

                if (result != null)
                {
                    int total = Convert.ToInt32(result.TotalPOs);
                    int pending = Convert.ToInt32(result.PendingPOs);
                    int received = Convert.ToInt32(result.ReceivedPOs);
                    decimal totalValue = result.TotalPOValue != null ? Convert.ToDecimal(result.TotalPOValue) : 0;

                    return L($"🛒 **Purchase Orders ki Surat-e-Haal:**\n\n" +
                             $"• Kul POs: {total}\n" +
                             $"• Pending POs: {pending}\n" +
                             $"• Received POs: {received}\n" +
                             $"• Kul PO Value: Rs. {totalValue:N0}\n\n" +
                             $"Received purchases ke liye 'received purchases' likhein.",
                             $"🛒 **Purchase Orders Status:**\n\n" +
                             $"• Total POs: {total}\n" +
                             $"• Pending POs: {pending}\n" +
                             $"• Received POs: {received}\n" +
                             $"• Total PO Value: Rs. {totalValue:N0}\n\n" +
                             $"For received purchases type 'received purchases'.");
                }

                return L("Purchase orders status nahi mil saka.", "Purchase orders status could not be found.");
            }
            catch (Exception ex)
            {
                return L($"Purchase orders check mein issue: {ex.Message}",
                         $"Issue while checking purchase orders: {ex.Message}");
            }
        }

        private async Task<string> GetReceivedPurchases()
        {
            try
            {
                string query = @"
                    SELECT TOP 5
                        po.ReferenceNo,
                        s.Name as SupplierName,
                        p.PurchaseReceivedAt as ReceivedDate,
                        p.SubTotal as Amount
                    FROM Purchase.Purchases p
                    INNER JOIN Purchase.PurchaseOrders po ON p.PurchaseOrderID = po.ID
                    INNER JOIN Supplier.Suppliers s ON po.SupplierID = s.ID
                    WHERE po.IsPurchaseReceived = 1
                    ORDER BY p.PurchaseReceivedAt DESC";

                var results = await DBHelper.GetListAsync<dynamic>(query: query);

                if (results != null && results.Any())
                {
                    var response = L("📦 **Haal mein Wasool Purchases:**\n\n",
                                     "📦 **Recently Received Purchases:**\n\n");

                    foreach (var row in results)
                    {
                        string refNo = row.ReferenceNo?.ToString() ?? "";
                        string supplier = row.SupplierName?.ToString() ?? "";
                        DateTime receivedDate = Convert.ToDateTime(row.ReceivedDate);
                        decimal amount = Convert.ToDecimal(row.Amount);

                        response += L($"**PO: {refNo}**\n" +
                                      $"• Supplier: {supplier}\n" +
                                      $"• Wasool Tareekh: {receivedDate:dd-MMM-yyyy}\n" +
                                      $"• Raqam: Rs. {amount:N0}\n\n",
                                      $"**PO: {refNo}**\n" +
                                      $"• Supplier: {supplier}\n" +
                                      $"• Received: {receivedDate:dd-MMM-yyyy}\n" +
                                      $"• Amount: Rs. {amount:N0}\n\n");
                    }

                    return response;
                }
                else
                {
                    return L("Koi received purchase nahi mili.", "No received purchase found.");
                }
            }
            catch (Exception ex)
            {
                return L($"Received purchases check mein issue: {ex.Message}",
                         $"Issue while checking received purchases: {ex.Message}");
            }
        }

        #endregion 🔹 PURCHASE ORDER HANDLERS

        #region 🔹 SUPPLIER HANDLERS

        private async Task<string> HandleSupplierQuery(string prompt)
        {
            return await GetSuppliersSummary();
        }

        private async Task<string> GetSuppliersSummary()
        {
            try
            {
                string query = @"
                                 SELECT
                    COUNT(*) AS TotalSuppliers,
                    COUNT(DISTINCT City) AS Cities,
                    STRING_AGG(City, ', ') AS AllCities
                FROM (
                    SELECT DISTINCT City
                    FROM Supplier.Suppliers
                    WHERE IsDeleted = 0
                ) AS c;
                ";

                var result = await DBHelper.GetAsync<dynamic>(query: query);

                if (result != null)
                {
                    int total = Convert.ToInt32(result.TotalSuppliers);
                    int cities = Convert.ToInt32(result.Cities);
                    string allCities = result.AllCities?.ToString() ?? "";

                    var suppliersList = await GetSuppliersList();

                    string response = L($"🏢 **Suppliers ki Tafseel:**\n\n" +
                             $"• Kul Suppliers: {total}\n" +
                             $"• Shehar: {cities}\n" +
                             $"• Locations: {allCities}\n\n",
                             $"🏢 **Suppliers Summary:**\n\n" +
                             $"• Total Suppliers: {total}\n" +
                             $"• Cities: {cities}\n" +
                             $"• Locations: {allCities}\n\n");

                    if (suppliersList.Any())
                    {
                        response += L("**Ahem Suppliers:**\n", "**Top Suppliers:**\n");
                        foreach (var supplier in suppliersList.Take(8))
                        {
                            response += $"• {supplier}\n";
                        }
                    }

                    return response;
                }

                return L("Suppliers summary nahi mil saka.", "Suppliers summary could not be found.");
            }
            catch (Exception ex)
            {
                return L($"Suppliers check mein issue: {ex.Message}",
                         $"Issue while checking suppliers: {ex.Message}");
            }
        }

        private async Task<List<string>> GetSuppliersList()
        {
            try
            {
                string query = @"
                    SELECT TOP 10 Name, City, Phone
                    FROM Supplier.Suppliers
                    WHERE IsDeleted = 0
                    ORDER BY Name";

                var results = await DBHelper.GetListAsync<dynamic>(query: query);
                return results.Select(r => $"{r.Name} ({r.City}) - {r.Phone}").ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Suppliers list error: {ex.Message}");
                return new List<string>();
            }
        }

        #endregion 🔹 SUPPLIER HANDLERS

        #region 🔹 MANUFACTURER & BRAND HANDLERS

        private async Task<string> HandleManufacturerQuery(string prompt)
        {
            return await GetManufacturersSummary();
        }

        private async Task<string> GetManufacturersSummary()
        {
            try
            {
                string query = @"
                  SELECT
                (SELECT COUNT(*) 
                 FROM Manufacturer.Manufacturers 
                 WHERE IsDeleted = 0) AS TotalManufacturers,
                (SELECT COUNT(DISTINCT City) 
                 FROM Manufacturer.Manufacturers 
                 WHERE IsDeleted = 0) AS Cities,
                (
                    SELECT STRING_AGG(Name, ', ')
                    FROM (
                        SELECT DISTINCT Name
                        FROM Manufacturer.Manufacturers
                        WHERE IsDeleted = 0
                    ) AS t
                ) AS ManufacturerNames;
            ";

                var result = await DBHelper.GetAsync<dynamic>(query: query);

                if (result != null)
                {
                    int total = Convert.ToInt32(result.TotalManufacturers);
                    int cities = Convert.ToInt32(result.Cities);
                    string names = result.ManufacturerNames?.ToString() ?? "";

                    var brands = await GetBrandsList();

                    string response = L($"🏭 **Manufacturers ki Tafseel:**\n\n" +
                             $"• Kul Manufacturers: {total}\n" +
                             $"• Shehar: {cities}\n" +
                             $"• Manufacturers: {names}\n\n",
                             $"🏭 **Manufacturers Summary:**\n\n" +
                             $"• Total Manufacturers: {total}\n" +
                             $"• Cities: {cities}\n" +
                             $"• Manufacturers: {names}\n\n");

                    if (brands.Any())
                    {
                        response += L("**Mojood Brands:**\n", "**Available Brands:**\n");
                        foreach (var brand in brands.Take(10))
                        {
                            response += $"• {brand}\n";
                        }
                    }

                    return response;
                }

                return L("Manufacturers summary nahi mil saka.", "Manufacturers summary could not be found.");
            }
            catch (Exception ex)
            {
                return L($"Manufacturers check mein issue: {ex.Message}",
                         $"Issue while checking manufacturers: {ex.Message}");
            }
        }

        private async Task<List<string>> GetBrandsList()
        {
            try
            {
                string query = @"
                    SELECT DISTINCT mb.Title as BrandName, m.Name as ManufacturerName
                    FROM Manufacturer.ManufacturerBrands mb
                    INNER JOIN Manufacturer.Manufacturers m ON mb.ManufacturerID = m.ID
                    ORDER BY mb.Title";

                var results = await DBHelper.GetListAsync<dynamic>(query: query);
                return results.Select(r => $"{r.BrandName} ({r.ManufacturerName})").ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Brands list error: {ex.Message}");
                return new List<string>();
            }
        }

        #endregion 🔹 MANUFACTURER & BRAND HANDLERS

        #region 🔹 EXPIRY & REPORT HANDLERS

        private async Task<string> HandleExpiryQuery(string prompt)
        {
            return await GetExpiryReport();
        }

        private async Task<string> GetExpiryReport()
        {
            try
            {
                string query = @"
                    SELECT
                        i.Name as ProductName,
                        v.VariationName,
                        v.ExpiryDate,
                        v.OpeningStock as Stock,
                        DATEDIFF(day, GETDATE(), v.ExpiryDate) as DaysUntilExpiry
                    FROM Inventory.InventoryItemVariations v
                    INNER JOIN Inventory.InventoryItems i ON v.ItemID = i.ID
                    WHERE v.ExpiryDate IS NOT NULL
                    AND v.ExpiryDate <= DATEADD(month, 3, GETDATE())
                    AND i.IsDeleted = 0
                    ORDER BY v.ExpiryDate ASC";

                var results = await DBHelper.GetListAsync<dynamic>(query: query);

                if (results != null && results.Any())
                {
                    var expiringSoon = results.Where(r => Convert.ToInt32(r.DaysUntilExpiry) <= 90).ToList();

                    var response = L("⚠️ **Expiry Report (Aaglay 3 Months):**\n\n",
                                     "⚠️ **Expiry Report (Next 3 Months):**\n\n");

                    foreach (var row in expiringSoon.Take(10))
                    {
                        string productName = row.ProductName?.ToString() ?? "";
                        string variation = row.VariationName?.ToString() ?? "";
                        DateTime expiry = Convert.ToDateTime(row.ExpiryDate);
                        int stock = Convert.ToInt32(row.Stock);
                        int daysLeft = Convert.ToInt32(row.DaysUntilExpiry);

                        response += L($"**{productName}** ({variation})\n" +
                                      $"• Expiry: {expiry:dd-MMM-yyyy}\n" +
                                      $"• Baqi Din: {daysLeft}\n" +
                                      $"• Stock: {stock}\n\n",
                                      $"**{productName}** ({variation})\n" +
                                      $"• Expiry: {expiry:dd-MMM-yyyy}\n" +
                                      $"• Days Left: {daysLeft}\n" +
                                      $"• Stock: {stock}\n\n");
                    }

                    response += L($"Total {expiringSoon.Count} products jald expire honay walay hain.",
                                  $"Total {expiringSoon.Count} products expiring soon.");

                    return response;
                }
                else
                {
                    return L("Alhamdulillah! ✅ Koi product expiry ke kareeb nahi hai.",
                             "Alhamdulillah! ✅ No product is near expiry.");
                }
            }
            catch (Exception ex)
            {
                return L($"Expiry report check mein issue: {ex.Message}",
                         $"Issue while checking expiry report: {ex.Message}");
            }
        }

        private async Task<string> HandleReportQuery(string prompt)
        {
            if (ContainsAny(prompt, "out", "khatam"))
            {
                return await GetOutOfStockItems();
            }

            if (ContainsAny(prompt, "expiry", "expire"))
            {
                return await GetExpiryReport();
            }

            if (ContainsAny(prompt, "dashboard", "overall", "complete"))
            {
                return await GetCompleteDashboardReport();
            }

            return await GetCompleteDashboardReport();
        }

        private async Task<string> GetCompleteDashboardReport()
        {
            try
            {
                var stockSummary = await GetCompleteStockSummary();
                var patientSummary = await GetTotalPatients();
                var salesSummary = await GetCompleteSalesSummary();
                var purchaseSummary = await GetPurchaseOrdersStatus();
                var supplierSummary = await GetSuppliersSummary();
                var manufacturerSummary = await GetManufacturersSummary();

                return L($"📈 **Mukammal Dashboard Report:**\n\n" +
                         $"{stockSummary}\n\n" +
                         $"{patientSummary}\n\n" +
                         $"{salesSummary}\n\n" +
                         $"{purchaseSummary}\n\n" +
                         $"{supplierSummary}\n\n" +
                         $"{manufacturerSummary}",
                         $"📈 **Complete Dashboard Report:**\n\n" +
                         $"{stockSummary}\n\n" +
                         $"{patientSummary}\n\n" +
                         $"{salesSummary}\n\n" +
                         $"{purchaseSummary}\n\n" +
                         $"{supplierSummary}\n\n" +
                         $"{manufacturerSummary}");
            }
            catch (Exception ex)
            {
                return L($"Dashboard report mein issue: {ex.Message}",
                         $"Issue while generating dashboard report: {ex.Message}");
            }
        }

        #endregion 🔹 EXPIRY & REPORT HANDLERS

        #region 🔹 HELPER METHODS

        private bool ContainsAny(string text, params string[] terms)
        {
            return terms.Any(term =>
            {
                // Chotay words (jaise "hi", "po") ko full word match karein taake "which", "suppose" jaise
                // words se false match na ho
                if (term.Length <= 3)
                    return Regex.IsMatch(text, @"\b" + Regex.Escape(term) + @"\b");

                return text.Contains(term);
            });
        }

        private async Task<string> ExtractMedicineName(string prompt)
        {
            var lowerPrompt = prompt.ToLower();

            try
            {
                // Pehle database se actual medicine names fetch karein
                var dbMedicines = await GetMedicineNamesFromDatabase();

                // Database medicines check karein
                foreach (var medicine in dbMedicines)
                {
                    if (lowerPrompt.Contains(medicine.ToLower()))
                        return medicine;
                }

                // Common product keywords
                var medicineKeywords = new[] {
                    "medicine", "product", "item", "dawa", "drug", "tablet", "capsule", "syrup", "injection"
                };

                // Agar product keyword hai to first product return karein
                foreach (var keyword in medicineKeywords)
                {
                    if (lowerPrompt.Contains(keyword))
                    {
                        return dbMedicines.FirstOrDefault() ?? "sample";
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Product extraction error: {ex.Message}");

                // Fallback hardcoded names
                var medicines = new[] {
                    "sample", "product", "item"
                };

                foreach (var medicine in medicines)
                {
                    if (lowerPrompt.Contains(medicine))
                        return medicine;
                }

                return null;
            }
        }

        private async Task<List<string>> GetMedicineNamesFromDatabase()
        {
            try
            {
                string query = @"
                    SELECT DISTINCT TOP 20 Name
                    FROM Inventory.InventoryItems
                    WHERE IsDeleted = 0
                    AND Name IS NOT NULL
                    ORDER BY Name";

                var results = await DBHelper.GetListAsync<dynamic>(query: query);

                var medicineNames = new List<string>();
                foreach (var row in results)
                {
                    if (row.Name != null)
                        medicineNames.Add(row.Name.ToString());
                }

                return medicineNames;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Get medicine names error: {ex.Message}");
                return new List<string> {
                    "panadol", "paracetamol", "brufen", "calpol", "diazepam",
                    "omeprazole", "atorvastatin", "amoxicillin", "insulin", "vitamin"
                };
            }
        }

        private async Task<bool> IsOnlyMedicineQuery(string prompt, string medicineName)
        {
            var words = prompt.Split(' ');

            // Agar sirf 1-2 words hain aur wo medicine name hai
            if (words.Length <= 2)
            {
                return true;
            }

            // Common non-stock words jo allow hain
            var allowedWords = new[] { "ka", "ki", "ke", "hai", "he", "what", "is", "the", "kya" };

            // Check karein ke koi stock-related word to nahi hai
            var stockWords = new[] { "stock", "inventory", "available", "quantity", "kitna", "bacha", "maal", "item", "detail", "information" };

            foreach (var word in words)
            {
                if (stockWords.Contains(word) && word != medicineName)
                {
                    return false;
                }
            }

            return true;
        }

        private async Task<List<string>> FindSimilarMedicines(string medicineName)
        {
            try
            {
                string query = @"
            SELECT TOP 5 Name
            FROM Inventory.InventoryItems
            WHERE Name LIKE @Pattern
            AND IsDeleted = 0
            ORDER BY Name";

                var parameters = new Dictionary<string, string>
        {
            { "@Pattern", $"%{medicineName}%" }
        };

                var results = await DBHelper.GetListAsync<dynamic>(query: query, Params: parameters);

                var medicineList = new List<string>();
                foreach (var row in results)
                {
                    if (row.Name != null && !string.IsNullOrEmpty(row.Name.ToString()))
                    {
                        medicineList.Add(row.Name.ToString());
                    }
                }

                return medicineList;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Find similar medicines error: {ex.Message}");
                return new List<string>();
            }
        }

        private string ExtractPatientInfo(string prompt)
        {
            // Simple extraction - improve as needed
            var words = prompt.Split(' ');
            foreach (var word in words)
            {
                if (word.Length > 2 && !IsCommonPatientWord(word))
                    return word;
            }
            return null;
        }

        private bool IsCommonPatientWord(string word)
        {
            var commonWords = new[] { "patient", "customer", "mariz", "bimar", "history", "report", "visit", "mr", "no", "mrn" };
            return commonWords.Contains(word.ToLower());
        }

        private string GetGreetingResponse()
        {
            var greetingsUrdu = new[]
            {
                "Assalam-o-Alaikum! 👋 Main aapka POS Assistant hun. Aap mujh se yeh cheeze poochh sakte hain:\n• Stock aur inventory\n• Customer information\n• Sales aur invoices\n• Purchases aur suppliers\n• Manufacturers\n• Reports aur summaries\n\nKaisi help chahiye aapko?",
                "Hello! 😊 POS Assistant hun. Main aapko in cheezon mein help kar sakta hun:\n• Stock status\n• Customer details\n• Sales reports\n• Purchase orders\n• Supplier info\n• Manufacturer data\n\nKya jan'na chahenge?",
                "Salam! 👍 Batayein aapko kya chahiye? Main aapko complete store information de sakta hun - stock, customers, sales, purchases, suppliers, manufacturers, aur reports!"
            };

            var greetingsEnglish = new[]
            {
                "Assalam-o-Alaikum! 👋 I am your POS Assistant. You can ask me about:\n• Stock and inventory\n• Customer information\n• Sales and invoices\n• Purchases and suppliers\n• Manufacturers\n• Reports and summaries\n\nHow can I help you?",
                "Hello! 😊 I am the POS Assistant. I can help you with:\n• Stock status\n• Customer details\n• Sales reports\n• Purchase orders\n• Supplier info\n• Manufacturer data\n\nWhat would you like to know?",
                "Hi! 👍 Tell me what you need? I can give you complete store information - stock, customers, sales, purchases, suppliers, manufacturers and reports!"
            };

            var pool = _lang == "ur" ? greetingsUrdu : greetingsEnglish;
            return pool[new Random().Next(pool.Length)];
        }

        private async Task<string> GetDefaultAIResponse(string prompt)
        {
            // Simple AI responses for general queries
            await Task.Delay(300);

            if (_lang == "ur")
            {
                return $"Main samjha aap poochh rahe hain: \"{prompt}\"\n\n" +
                       "Main in cheezon mein help kar sakta hun:\n" +
                       "• Stock aur inventory (total stock, low stock, out of stock)\n" +
                       "• Customer information (total customers, customer details, invoices)\n" +
                       "• Sales aur invoices (today sales, total sales, revenue)\n" +
                       "• Purchases aur suppliers (purchase orders, received purchases)\n" +
                       "• Manufacturers aur brands\n" +
                       "• Reports (expiry report, dashboard report)\n\n" +
                       "Specific sawal poochein jaise:\n" +
                       "'total stock', 'customer ali', 'today sales', 'purchase orders', 'suppliers', 'manufacturers', 'expiry report'";
            }

            return $"I understand you are asking: \"{prompt}\"\n\n" +
                   "I can help you with:\n" +
                   "• Stock and inventory (total stock, low stock, out of stock)\n" +
                   "• Customer information (total customers, customer details, invoices)\n" +
                   "• Sales and invoices (today sales, total sales, revenue)\n" +
                   "• Purchases and suppliers (purchase orders, received purchases)\n" +
                   "• Manufacturers and brands\n" +
                   "• Reports (expiry report, dashboard report)\n\n" +
                   "Ask a specific question such as:\n" +
                   "'total stock', 'customer ali', 'today sales', 'purchase orders', 'suppliers', 'manufacturers', 'expiry report'";
        }

        #endregion 🔹 HELPER METHODS

        public async Task<bool> LogQuery(string query, string response, bool isFinal = false)
        {
            try
            {
                Dictionary<string, string> dbValues = new Dictionary<string, string>
                {
                    { "@Query", query },
                    { "@Response", response },
                    { "@Timestamp", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") }
                };

                string insertQuery = "INSERT INTO Chatbot.ChatbotLogs (Query, Response, Timestamp) VALUES (@Query, @Response, @Timestamp)";
                return await DBHelper.ExecuteQueryAsync(insertQuery, dbValues);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Logging Error: {ex.Message}");
                return false;
            }
        }
    }
}
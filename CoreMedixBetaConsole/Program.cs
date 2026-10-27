using CoreMedixBeta.Models.Sales;
using CoreMedixBeta.Models.Sales.DTOs;
using CoreMedixBeta.Repositories;
using System;
using System.Collections.Generic;

namespace CoreMedixBetaConsole
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            Console.WriteLine("🚀 Starting QA tests...");

            try
            {
                var repo = new SalesRepository();

                var result = repo.SaveSaleAsync(new SaveSaleInputDto
                {
                    SaleInvoice = new SaleInvoice
                    {
                        CustomerID = "1",
                        InvoiceDate = DateTime.Now
                    },
                    SaleInvoiceItems = new List<SaleInvoiceItem>
                    {
                        new SaleInvoiceItem { ItemID = 1, Qty = 2, LineTotal = 200 }
                    },
                    SaleInvoicePayments = new List<SaleInvoicePayment>
                    {
                        new SaleInvoicePayment { Date = DateTime.Now, PaidAmount = 200, PaymentMode = "Cash" }
                    }
                }).Result;

                Console.WriteLine("✅ SaveSale test passed! Result: " + result);
            }
            catch (Exception ex)
            {
                Console.WriteLine("❌ Test failed: " + ex.Message);
            }

            Console.WriteLine("QA finished. Press any key to exit...");
            Console.ReadKey();
        }
    }
}
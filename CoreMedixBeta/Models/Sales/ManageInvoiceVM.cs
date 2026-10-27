using CoreMedixBeta.Models.Discounts.DTOs;
using CoreMedixBeta.Models.InventoryItems.DTOs;
using CoreMedixBeta.Models.Patient;
using CoreMedixBeta.Models.Sales.DTOs;
using CoreMedixBeta.Models.Supplier;
using CoreMedixBeta.Models.Taxes.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CoreMedixBeta.Models.Sales
{
    public class ManageInvoiceVM
    {
        public GetSaleInvoiceOutputDto Invoice { get; set; } = new GetSaleInvoiceOutputDto();
        public List<GetSupplierShortListOutputDto> Suppliers { get; set; }
        public List<GetManufacturerShortListOutputDto> Manufacturers { get; set; }
        public List<TaxesShortListOutputDto> Taxes { get; set; } = new List<TaxesShortListOutputDto>();
        public List<GetDiscountsShortOutputDto> Discounts { get; set; } = new List<GetDiscountsShortOutputDto>();

        public List<string> Cities { get; set; }
        public Patients Patient { get; set; }
        public int InvoiceID { get; set; }
        public string InvoicePrefix { get; set; }

        /// <summary>
        /// Following 4 Fields will be used in case of converting the
        /// Referral Order to Invoice
        /// </summary>
        public int ReferralOrderID { get; set; }

        public string ReferredByID { get; set; }
        public string ReferredByType { get; set; }
        public string ReferredByName { get; set; }
        public string CustomerID { get; set; }
        public string CustomerType { get; set; }
        public string CustomerName { get; set; }
    }
}
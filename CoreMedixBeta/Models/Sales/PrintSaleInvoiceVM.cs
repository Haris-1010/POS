using CoreMedixBeta.DbModels;
using CoreMedixBeta.Models.Sales.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CoreMedixBeta.Models.Sales
{
    public class PrintSaleInvoiceVM : BasePrintVM
    {
        public GetInvoiceDetailForPrintOutputDto InvoiceDetail { get; set; } = new GetInvoiceDetailForPrintOutputDto();
    }
}
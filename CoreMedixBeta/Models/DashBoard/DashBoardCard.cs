using CoreMedixBeta.Models.InventoryItems.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CoreMedixBeta.Models.DashBoard
{
    public class DashBoardVM
    {
        public GetDashBoardCardsOutputDto DashBoardCardsValues { get; set; }
        public List<DashBoardCard> DashBoardCards { get; set; }
        public List<DashboardSetting> DashboardSettings { get; set; }
        public DashboardChartsDto Charts { get; set; }
    }

    public class DashBoardCard
    {
        public int CardID { get; set; }
        public string Title { get; set; }
        public string Value { get; set; }
        public string BgColor { get; set; }
        public string Icon { get; set; }
        public int Order { get; set; }
        public bool IsVisible { get; set; } = true;
    }

    public class DashboardChartsDto
    {
        public DashboardChartsDto()
        {
            DailySales = new List<DailySalePoint>();
            TopProducts = new List<LabelValueItem>();
            MonthlySales = new List<LabelValueItem>();
            PaymentModes = new List<LabelValueItem>();
            InventoryHealth = new List<LabelValueItem>();
        }

        public List<DailySalePoint> DailySales { get; set; }
        public List<LabelValueItem> TopProducts { get; set; }
        public List<LabelValueItem> MonthlySales { get; set; }
        public List<LabelValueItem> PaymentModes { get; set; }

        /// <summary>Total Products / Low Stock / Expiring Soon - live snapshot, not date-filtered.</summary>
        public List<LabelValueItem> InventoryHealth { get; set; }

        /// <summary>"day" | "month" | "year" - how DailySales points are bucketed for the selected date range.</summary>
        public string TrendGranularity { get; set; } = "day";
    }

    public class DailySalePoint
    {
        public string Date { get; set; }
        public decimal SaleAmount { get; set; }
        public decimal ProfitAmount { get; set; }
    }

    public class LabelValueItem
    {
        public string Label { get; set; }
        public decimal Value { get; set; }
    }
}
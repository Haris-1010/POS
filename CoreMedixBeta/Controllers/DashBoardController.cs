using CoreMedixBeta.DbModels;
using CoreMedixBeta.Models.DashBoard;
using CoreMedixBeta.Models.DashBoard.DTOs;
using CoreMedixBeta.Models.Discounts.DTOs;
using CoreMedixBeta.Models.InventoryItems.DTOs;
using CoreMedixBeta.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Caching;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;

namespace CoreMedixBeta.Controllers
{
    [Authorize]
    public class DashBoardController : Controller
    {
        private readonly DashBoardRepository _dashBoardRepo;

        public DashBoardController() : this(new DashBoardRepository())
        {
        }

        public DashBoardController(DashBoardRepository dashBoard)
        {
            _dashBoardRepo = dashBoard;
        }

        public async Task<ActionResult> Dashboard()
        {
            try
            {
                var range = await ResolveDateRangeAsync("today", null, null);

                var dbCard = await _dashBoardRepo.GetDashBoardCardsAsync(new RequestByMasterUsernameDTO(), range.From, range.To);
                var userSettings = await _dashBoardRepo.GetUserDashboardSettingsAsync(new RequestByMasterUsernameDTO());
                var charts = await _dashBoardRepo.GetDashboardChartsAsync(range.From, range.To);
                AttachInventoryHealthChart(charts, dbCard);

                var model = new DashBoardVM
                {
                    DashBoardCardsValues = dbCard,
                    DashBoardCards = BuildFilteredCards(dbCard, userSettings),
                    Charts = charts,
                    DashboardSettings = userSettings.Select(s => new DashboardSetting
                    {
                        ID = s.ID,
                        CardID = s.CardID,
                        Title = s.Title,
                        Order = s.Order,
                        IsVisible = s.IsVisible,
                        UserID = s.UserID,
                    }).ToList()
                };

                return View(model);
            }
            catch (Exception)
            {
                return View(new DashBoardVM { DashBoardCards = new List<DashBoardCard>(), Charts = new DashboardChartsDto() });
            }
        }

        /// <summary>
        /// Refreshes the stat cards + charts for a date-range filter selected on the dashboard
        /// (Today / Yesterday / This Week / Last Week / This Month / Last Month / Custom / All Time)
        /// without a full page reload.
        /// </summary>
        [HttpPost]
        public async Task<JsonResult> GetDashboardData(GetDashboardDataInputDto input)
        {
            try
            {
                var range = await ResolveDateRangeAsync(input?.Range, input?.FromDate, input?.ToDate);

                var dbCard = await _dashBoardRepo.GetDashBoardCardsAsync(new RequestByMasterUsernameDTO(), range.From, range.To);
                var userSettings = await _dashBoardRepo.GetUserDashboardSettingsAsync(new RequestByMasterUsernameDTO());
                var charts = await _dashBoardRepo.GetDashboardChartsAsync(range.From, range.To);
                AttachInventoryHealthChart(charts, dbCard);

                return Json(new AjaxRequestDTO
                {
                    Status = true,
                    Message = "Dashboard data refreshed",
                    Data = new
                    {
                        Cards = BuildFilteredCards(dbCard, userSettings),
                        Charts = charts,
                        RangeLabel = range.Label,
                        FromDate = range.From.ToString("yyyy-MM-dd"),
                        ToDate = range.To.ToString("yyyy-MM-dd")
                    }
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return Json(new AjaxRequestDTO { Status = false, Message = "Failed to load dashboard data for that range." });
            }
        }

        /// <summary>
        /// Maps a filter key (and, for "custom", an explicit from/to) to an actual [From, To]
        /// date range (inclusive, date-only) plus a human-readable label for the UI pill.
        /// </summary>
        private async Task<(DateTime From, DateTime To, string Label)> ResolveDateRangeAsync(string rangeKey, DateTime? customFrom, DateTime? customTo)
        {
            DateTime today = DateTime.Now.Date;
            string key = (rangeKey ?? "today").Trim().ToLowerInvariant();

            switch (key)
            {
                case "yesterday":
                    return (today.AddDays(-1), today.AddDays(-1), "Yesterday");

                case "thisweek":
                    {
                        int diff = ((int)today.DayOfWeek - (int)DayOfWeek.Monday + 7) % 7;
                        DateTime start = today.AddDays(-diff);
                        return (start, today, "This Week");
                    }

                case "lastweek":
                    {
                        int diff = ((int)today.DayOfWeek - (int)DayOfWeek.Monday + 7) % 7;
                        DateTime thisWeekStart = today.AddDays(-diff);
                        DateTime lastWeekStart = thisWeekStart.AddDays(-7);
                        DateTime lastWeekEnd = thisWeekStart.AddDays(-1);
                        return (lastWeekStart, lastWeekEnd, "Last Week");
                    }

                case "thismonth":
                    {
                        DateTime start = new DateTime(today.Year, today.Month, 1);
                        return (start, today, "This Month");
                    }

                case "lastmonth":
                    {
                        DateTime thisMonthStart = new DateTime(today.Year, today.Month, 1);
                        DateTime lastMonthStart = thisMonthStart.AddMonths(-1);
                        DateTime lastMonthEnd = thisMonthStart.AddDays(-1);
                        return (lastMonthStart, lastMonthEnd, "Last Month");
                    }

                case "custom":
                    {
                        DateTime from = (customFrom ?? today).Date;
                        DateTime to = (customTo ?? today).Date;
                        if (to < from)
                        {
                            DateTime swap = from;
                            from = to;
                            to = swap;
                        }
                        return (from, to, $"{from:dd MMM yyyy} – {to:dd MMM yyyy}");
                    }

                case "alltime":
                    {
                        DateTime? earliest = await _dashBoardRepo.GetEarliestSaleDateAsync();
                        DateTime start = earliest ?? today.AddYears(-1);
                        return (start, today, "All Time");
                    }

                case "today":
                default:
                    return (today, today, "Today");
            }
        }

        /// <summary>Populates the Inventory Health bar chart from the same live snapshot values used by the stat cards.</summary>
        private static void AttachInventoryHealthChart(DashboardChartsDto charts, GetDashBoardCardsOutputDto dbCard)
        {
            charts.InventoryHealth = new List<LabelValueItem>
            {
                new LabelValueItem { Label = "Total Products", Value = dbCard.TotalItems },
                new LabelValueItem { Label = "Low Stock", Value = dbCard.LowStockItems },
                new LabelValueItem { Label = "Expiring Soon", Value = dbCard.TotalExpiryItems }
            };
        }

        /// <summary>Builds the 8 stat cards from live values and applies the user's visibility/order preferences.</summary>
        private static List<DashBoardCard> BuildFilteredCards(GetDashBoardCardsOutputDto dbCard, List<GetDashboardSettingOutputDto> userSettings)
        {
            var dashboardCards = new List<DashBoardCard>
            {
                new DashBoardCard { CardID = 1, Title = "Sales", Value = Math.Round(dbCard.TotalSaleToday, 0, MidpointRounding.AwayFromZero).ToString("N0"), BgColor = "bg-white", Icon = "fas fa-hand-holding-usd" },
                new DashBoardCard { CardID = 2, Title = "Total Invoices", Value = dbCard.TotalInvoicesToday.ToString(), BgColor = "bg-white", Icon = "fas fa-file-invoice-dollar" },
                new DashBoardCard { CardID = 3, Title = "Profit", Value = Math.Round(dbCard.TotalProfitToday, 0, MidpointRounding.AwayFromZero).ToString("N0"), BgColor = "bg-white", Icon = "fas fa-chart-line" },
                new DashBoardCard { CardID = 4, Title = "Total Customers", Value = dbCard.TotalCustomers.ToString(), BgColor = "bg-white", Icon = "fas fa-users" },
                new DashBoardCard { CardID = 5, Title = "Stock Value", Value = Math.Round(dbCard.TotalStockValue, 0, MidpointRounding.AwayFromZero).ToString("N0"), BgColor = "bg-white", Icon = "fas fa-boxes" },
                new DashBoardCard { CardID = 6, Title = "Expiring Soon", Value = dbCard.TotalExpiryItems.ToString(), BgColor = "bg-white", Icon = "fas fa-exclamation-triangle" },
                new DashBoardCard { CardID = 7, Title = "Stock Sold Value", Value = dbCard.TodayStockSoldValue.ToString(), BgColor = "bg-white", Icon = "fas fa-cubes" },
                new DashBoardCard { CardID = 8, Title = "Amount Receivable", Value = Math.Round(dbCard.TotalReceivable, 0, MidpointRounding.AwayFromZero).ToString("N0"), BgColor = "bg-white", Icon = "fas fa-wallet" }
            };

            if (userSettings != null && userSettings.Count > 0)
            {
                // Cards the user has never seen before (e.g. a newly added one) have no saved
                // setting row - default them to visible instead of dropping them via an inner join.
                return dashboardCards
                    .Select(card =>
                    {
                        var setting = userSettings.FirstOrDefault(s => s.CardID == card.CardID);
                        return new DashBoardCard
                        {
                            CardID = card.CardID,
                            Title = card.Title,
                            Value = card.Value,
                            BgColor = card.BgColor,
                            Icon = card.Icon,
                            Order = setting?.Order ?? card.CardID,
                            IsVisible = setting?.IsVisible ?? true
                        };
                    })
                    .Where(card => card.IsVisible)
                    .OrderBy(card => card.Order)
                    .ToList();
            }

            return dashboardCards;
        }

        [HttpPost]
        public async Task<JsonResult> SaveDashBoardSetting(SaveDashboardSettingInputDto input)
        {
            try
            {
                if (!ModelState.IsValid)
                    return Json(new AjaxRequestDTO() { Status = false, Message = "Unable to process your request due to invalid data" });

                var isSave = await _dashBoardRepo.SavetDashBoardSettingAsync(input);
                if (isSave)
                    return Json(new AjaxRequestDTO { Status = true, Message = "Dashboard Settings Saved Successfully" });

                return Json(new AjaxRequestDTO { Status = false, Message = "Failed to Save Dashboard Settings due to technical error" });
            }
            catch (Exception ex)
            {
                return Json(new AjaxRequestDTO() { Status = false, Message = ex.Message });
            }
        }

        public ActionResult AccessDenied() => View();

        [HttpPost]
        public JsonResult LogOut()
        {
            ExpireSessionAndCookies();
            return Json(new
            {
                Status = true,
                RedirectUrl = "/Account/Login",
                Message = "You have been logged out successfully."
            });
        }

        private void ExpireSessionAndCookies()
        {
            Session.Abandon();
            Session.Clear();
            FormsAuthentication.SignOut();

            if (Request.Cookies[FormsAuthentication.FormsCookieName] != null)
            {
                HttpCookie authCookie = new HttpCookie(FormsAuthentication.FormsCookieName, "");
                authCookie.Expires = DateTime.Now.AddDays(-1);
                Response.Cookies.Add(authCookie);
            }
        }
    }
}
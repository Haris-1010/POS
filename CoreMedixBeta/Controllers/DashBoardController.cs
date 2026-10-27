using CoreMedixBeta.DbModels;
using CoreMedixBeta.Models.DashBoard;
using CoreMedixBeta.Models.DashBoard.DTOs;
using CoreMedixBeta.Models.Discounts.DTOs;
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
                var dbCard = await _dashBoardRepo.GetDashBoardCardsAsync(new RequestByMasterUsernameDTO
                {
                });

                var userSettings = await _dashBoardRepo.GetUserDashboardSettingsAsync(new RequestByMasterUsernameDTO
                {
                });

                var charts = await _dashBoardRepo.GetDashboardChartsAsync();

                var dashboardCards = new List<DashBoardCard>
                {
                    new DashBoardCard { CardID = 1, Title = "Today’s Sales", Value = Math.Round(dbCard.TotalSaleToday, 0, MidpointRounding.AwayFromZero).ToString("N0"), BgColor = "bg-white", Icon = "fas fa-hand-holding-usd" },
                    new DashBoardCard { CardID = 2, Title = "Today Total Invoices", Value = dbCard.TotalInvoicesToday.ToString(), BgColor = "bg-white", Icon = "fas fa-file-invoice-dollar" },
                    new DashBoardCard { CardID = 3, Title = "Today’s Profit", Value = Math.Round(dbCard.TotalProfitToday, 0, MidpointRounding.AwayFromZero).ToString("N0"), BgColor = "bg-white", Icon = "fas fa-chart-line" },
                    new DashBoardCard { CardID = 4, Title = "Total Patients Today", Value = dbCard.TotalCustomers.ToString(), BgColor = "bg-white", Icon = "fas fa-users" },
                    new DashBoardCard { CardID = 5, Title = "Stock Value", Value = Math.Round(dbCard.TotalStockValue, 0, MidpointRounding.AwayFromZero).ToString("N0"), BgColor = "bg-white", Icon = "fas fa-boxes" },
                    new DashBoardCard { CardID = 6, Title = "Expiring Soon", Value = dbCard.TotalExpiryItems.ToString(), BgColor = "bg-white", Icon = "fas fa-exclamation-triangle" },
                    new DashBoardCard { CardID = 7, Title = "Today’s Stock Sold Value", Value = dbCard.TodayStockSoldValue.ToString(), BgColor = "bg-white", Icon = "fas fa-cubes" }
                };

                List<DashBoardCard> filteredCards;

                if (userSettings != null && userSettings.Count > 0)
                {
                    // Apply User Preferences
                    filteredCards = dashboardCards
                        .Join(userSettings, card => card.CardID, setting => setting.CardID,
                            (card, setting) => new DashBoardCard
                            {
                                CardID = card.CardID,
                                Title = card.Title,
                                Value = card.Value,
                                BgColor = card.BgColor,
                                Icon = card.Icon,
                                Order = setting.Order
                            })
                        .Where(card => userSettings.First(s => s.CardID == card.CardID).IsVisible ?? false)
                        .OrderBy(card => card.Order)
                        .ToList();
                }
                else
                {
                    // No user settings: return default cards
                    filteredCards = dashboardCards;
                }

                var model = new DashBoardVM
                {
                    DashBoardCardsValues = dbCard,
                    DashBoardCards = filteredCards,
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
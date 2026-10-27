using CoreMedixBeta.Repositories;
using System;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace CoreMedixBeta.Controllers
{
    [Authorize]
    public class ChatbotController : Controller
    {
        private readonly ChatbotRepository _chatbotRepository;

        public ChatbotController() : this(new ChatbotRepository())
        {
        }

        public ChatbotController(ChatbotRepository chatbotRepository)
        {
            _chatbotRepository = chatbotRepository;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<JsonResult> QueryAI(string prompt)
        {
            if (string.IsNullOrEmpty(prompt))
                return Json(new { Status = false, Message = "Please enter a message." });

            try
            {
                // Call AI service
                string aiResponse = await _chatbotRepository.GetAIResponse(prompt);

                // Optionally log the query and response
                await _chatbotRepository.LogQuery(prompt, aiResponse);

                return Json(new { Status = true, Message = aiResponse });
            }
            catch (Exception ex)
            {
                // Log error properly
                System.Diagnostics.Debug.WriteLine($"Chatbot Error: {ex.Message}");
                return Json(new { Status = false, Message = "Sorry, I'm having trouble responding. Please try again." });
            }
        }

        // Optional: Get method for testing
        [HttpGet]
        public ActionResult Test()
        {
            return Content("Chatbot controller is working!");
        }
    }
}
using Azure.Messaging.ServiceBus;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Logging;
using Umbraco.Cms.Core.Routing;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Web;
using Umbraco.Cms.Infrastructure.Persistence;
using Umbraco.Cms.Web.Website.Controllers;
using UmbracoProject.Models;

namespace UmbracoProject.Controllers
{
    public class ContactSurfaceController : SurfaceController
    {

        private readonly IConfiguration _configuration;
        public ContactSurfaceController(IUmbracoContextAccessor umbracoContextAccessor, IUmbracoDatabaseFactory databaseFactory, ServiceContext services, AppCaches appCaches, IProfilingLogger profilingLogger, IPublishedUrlProvider publishedUrlProvider, IConfiguration configuration) : base(umbracoContextAccessor, databaseFactory, services, appCaches, profilingLogger, publishedUrlProvider)
        {
            _configuration = configuration;
        }

        public async Task<IActionResult> HandleSubmit(ContactFormModel form)
        {
            if(!ModelState.IsValid)
            {

                ViewData["name"] = form.Name;
                ViewData["email"] = form.Email;

                ViewData["error_name"] = string.IsNullOrEmpty(form.Name);
                ViewData["error_email"] = string.IsNullOrEmpty(form.Email);

                return CurrentUmbracoPage();
            }

            string ServiceBus = _configuration["NewSettings:ServiceBus"]!;
            string queue = "email_request";

            ServiceBusClient client = new ServiceBusClient(ServiceBus);
            ServiceBusSender sender = client.CreateSender(queue);

            var request = GenerateMessage(form);

            var messageBody = JsonConvert.SerializeObject(request);

            ServiceBusMessage message = new ServiceBusMessage(messageBody);

            await sender.SendMessageAsync(message);


            ViewData["success"] = "form submitted succesfully";

            return CurrentUmbracoPage();
        }


        public ContactRequestModel GenerateMessage(ContactFormModel model)
        {
            if(model != null)
            {
                var contactRequest = new ContactRequestModel()
                {
                    To = model.Email,
                    Subject = $"Thank you for messaging us {model.Name}",
                    HtmlBody = $@"
                           <!DOCTYPE html>
                            <html lang=""sv"">
                            <head>
                                <meta charset=""UTF-8"">
                                <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
                                <title>Bekräftelse på ditt meddelande</title>
                                <style>
                                    /* Grundläggande styling */
                                    body {{
                                        font-family: Arial, sans-serif;
                                        background-color: #f4f4f4;
                                        color: #333333;
                                        margin: 0;
                                        padding: 0;
                                    }}

                                    .email-container {{
                                        max-width: 600px;
                                        margin: 0 auto;
                                        background-color: #ffffff;
                                        padding: 20px;
                                        border-radius: 8px;
                                        box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
                                    }}

                                    h1 {{
                                        color: #007BFF;
                                        font-size: 24px;
                                    }}

                                    p {{
                                        line-height: 1.6;
                                        font-size: 16px;
                                        margin-bottom: 20px;
                                    }}

                                    .highlight {{
                                        color: #007BFF;
                                        font-weight: bold;
                                    }}

                                    .footer {{
                                        font-size: 12px;
                                        color: #777777;
                                        text-align: center;
                                        margin-top: 30px;
                                    }}

                                    /* Mobilanpassning */
                                    @media (max-width: 600px) {{
                                        .email-container {{
                                            padding: 10px;
                                        }}

                                        h1 {{
                                            font-size: 20px;
                                        }}

                                        p {{
                                            font-size: 14px;
                                        }}
                                    }}
                                </style>
                            </head>
                            <body>
                                <div class=""email-container"">
                                    <h1>Tack för ditt meddelande!</h1>
                                    <p>Hej,{model.Name}</p>
                                    <p>Vi vill härmed bekräfta att ditt meddelande har skickats till <span class=""highlight"">Onatrix</span>. Vi uppskattar att du tog dig tid att kontakta oss och kommer att återkomma till dig så snart som möjligt.</p>
                                    <p>Om du har några frågor eller behöver ytterligare hjälp, tveka inte att kontakta oss på <a href=""mailto:support@onatrix.se"">support@onatrix.se</a>.</p>
                                    <p>Med vänliga hälsningar,<br>
                                    <strong>Onatrix</strong></p>

                                    <div class=""footer"">
                                        <p>Detta är ett automatiskt mejl, vänligen svara inte på detta.</p>
                                    </div>
                                </div>
                            </body>
                            </html> 
                    ",
                    PlainText = $"Hej,{model.Name}. Vi vill härmed bekräfta att ditt meddelande har skickats till Onatrix"
                };

                return contactRequest;
            }

            return null!;

        }
    }
}

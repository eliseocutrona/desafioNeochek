using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace NeoCheck.FraudPrevention.API.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class FraudPreventionController : ControllerBase
    {
        private readonly ILogger<FraudPreventionController> _logger;

        public FraudPreventionController(ILogger<FraudPreventionController> logger)
        {
            _logger = logger;
        }

        [HttpPost]
        [Route("validate")]
        public IActionResult Validate()
        {
            // Lee el contenido del archivo JSON
            string jsonString = System.IO.File.ReadAllText("../SampleRequest.json");

            // Deserializa el JSON en una lista de objetos PurchaseInformation
            List<PurchaseInformation> purchases = JsonSerializer.Deserialize<List<PurchaseInformation>>(jsonString);

            if (purchases == null || purchases.Count == 0)
            {
                return BadRequest("No se proporcionaron compras para validar.");
            }

            List<int> fraudulentOrderIds = DetectFraudulentOrders(purchases);

            return Ok(fraudulentOrderIds);
        }

        private List<int> DetectFraudulentOrders(List<PurchaseInformation> purchases)
        {
            // Ordena las compras por correo electrónico y luego por dirección
            var sortedPurchases = purchases.OrderBy(purchase => purchase.EmailAddress)
                                           .ThenBy(purchase => purchase.StreetAddress)
                                           .ToList();

            List<int> fraudulentOrderIds = new List<int>();

            // Itera sobre las compras ordenadas para identificar órdenes fraudulentas
            for (int i = 0; i < sortedPurchases.Count - 1; i++)
            {
                var currentPurchase = sortedPurchases[i];
                var nextPurchase = sortedPurchases[i + 1];

                if ((HaveSameEmailAndDeal(currentPurchase, nextPurchase) || HaveSameAddressAndDeal(currentPurchase, nextPurchase)) &&
                    currentPurchase.CreditCardNumber != nextPurchase.CreditCardNumber)
                {
                    // Si las compras son fraudulentas, agrega sus IDs a la lista
                    fraudulentOrderIds.Add((int)currentPurchase.OrderId);
                    fraudulentOrderIds.Add((int)nextPurchase.OrderId);
                }
            }

            // Elimina duplicados de la lista de IDs de órdenes fraudulentas
            fraudulentOrderIds = fraudulentOrderIds.Distinct().ToList();

            return fraudulentOrderIds;
        }

        private bool HaveSameEmailAndDeal(PurchaseInformation purchase1, PurchaseInformation purchase2)
        {
            return string.Equals(purchase1.EmailAddress, purchase2.EmailAddress, StringComparison.OrdinalIgnoreCase) &&
                   purchase1.DealId == purchase2.DealId;
        }

        private bool HaveSameAddressAndDeal(PurchaseInformation purchase1, PurchaseInformation purchase2)
        {
            return string.Equals(purchase1.StreetAddress, purchase2.StreetAddress, StringComparison.OrdinalIgnoreCase) &&
                   string.Equals(purchase1.City, purchase2.City, StringComparison.OrdinalIgnoreCase) &&
                   string.Equals(purchase1.State, purchase2.State, StringComparison.OrdinalIgnoreCase) &&
                   string.Equals(purchase1.ZipCode, purchase2.ZipCode, StringComparison.OrdinalIgnoreCase) &&
                   purchase1.DealId == purchase2.DealId;
        }
    }
}

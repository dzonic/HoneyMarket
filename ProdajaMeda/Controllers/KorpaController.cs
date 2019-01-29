using ProdajaMeda.Models.Data;
using ProdajaMeda.Models.ViewModels.Korpa;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Web;
using System.Web.Mvc;

namespace ProdajaMeda.Controllers
{
    public class KorpaController : Controller
    {
        // GET: Korpa
        public ActionResult Index()
        {

            var cart = Session["korpa"] as List<KorpaVM> ?? new List<KorpaVM>();

            // Provera da li je korpa prazna
            if (cart.Count == 0 || Session["korpa"] == null)
            {
                ViewBag.Message = "Vasa korpa je prazna.";
                return View();
            }

            decimal total = 0m;

            foreach (var item in cart)
            {
                total += item.Total;
            }
            ViewBag.GrandTotal = total;

            return View(cart);
        }

        public ActionResult CartPartial()
        {
            KorpaVM model = new KorpaVM();

            int qty = 0;

            decimal price = 0m;

            if (Session["korpa"] != null)
            {

                var list = (List<KorpaVM>)Session["korpa"];

                foreach (var item in list)
                {
                    qty += item.Quantity;
                    price += item.Quantity * item.Price;
                }

                model.Quantity = qty;
                model.Price = price;

            }
            else
            {
                model.Quantity = 0;
                model.Price = 0m;
            }

            return PartialView(model);
        }

        public ActionResult AddToCartPartial(int id)
        {
            List<KorpaVM> cart = Session["korpa"] as List<KorpaVM> ?? new List<KorpaVM>();

            KorpaVM model = new KorpaVM();

            using (Db db = new Db())
            {
                ProductDTO product = db.Products.Find(id);

                // Provera da li proizvod vec postoji u korpi
                var productInCart = cart.FirstOrDefault(x => x.ProductId == id);

                // ako ne postoji dodaje se novi
                if (productInCart == null)
                {
                    cart.Add(new KorpaVM()
                    {
                        ProductId = product.Id,
                        ProductName = product.Name,
                        Quantity = 1,
                        Price = product.Price,
                        Image = product.ImageName
                    });
                }
                else
                {
                    productInCart.Quantity++;
                }
            }
            int qty = 0;
            decimal price = 0m;

            foreach (var item in cart)
            {
                qty += item.Quantity;
                price += item.Quantity * item.Price;
            }

            model.Quantity = qty;
            model.Price = price;

            //Cuvanje
            Session["korpa"] = cart;

            return PartialView(model);
        }

        public JsonResult IncrementProduct(int productId)
        {
            List<KorpaVM> cart = Session["korpa"] as List<KorpaVM>;

            using (Db db = new Db())
            {
                KorpaVM model = cart.FirstOrDefault(x => x.ProductId == productId);

                // Inkrementiramo qty
                model.Quantity++;

                var result = new { qty = model.Quantity, price = model.Price };

                return Json(result, JsonRequestBehavior.AllowGet);
            }

        }

        public ActionResult DecrementProduct(int productId)
        {
            List<KorpaVM> cart = Session["korpa"] as List<KorpaVM>;

            using (Db db = new Db())
            {
                KorpaVM model = cart.FirstOrDefault(x => x.ProductId == productId);

                // Decrementacija qty
                if (model.Quantity > 1)
                {
                    model.Quantity--;
                }
                else
                {
                    model.Quantity = 0;
                    cart.Remove(model);
                }

                var result = new { qty = model.Quantity, price = model.Price };

                return Json(result, JsonRequestBehavior.AllowGet);
            }

        }

        public void RemoveProduct(int productId)
        {
            List<KorpaVM> cart = Session["korpa"] as List<KorpaVM>;

            using (Db db = new Db())
            {
                KorpaVM model = cart.FirstOrDefault(x => x.ProductId == productId);

                //Uklanjanje model iz listw
                cart.Remove(model);
            }

        }

        public ActionResult PaypalPartial()
        {
            List<KorpaVM> cart = Session["korpa"] as List<KorpaVM>;

            return PartialView(cart);
        }

        // POST: /korpa/PlaceOrder
        [HttpPost]
        public void PlaceOrder()
        {
            // Get cart list
            List<KorpaVM> cart = Session["korpa"] as List<KorpaVM>;

            // Get username
            string username = User.Identity.Name;

            int orderId = 0;

            using (Db db = new Db())
            {
                // Init OrderDTO
                OrderDTO orderDTO = new OrderDTO();

                // Get user id
                var q = db.Users.FirstOrDefault(x => x.Username == username);
                int userId = q.Id;

                // Add to OrderDTO and save
                orderDTO.UserId = userId;
                orderDTO.CreatedAt = DateTime.Now;

                db.Orders.Add(orderDTO);

                db.SaveChanges();

                // Get inserted id
                orderId = orderDTO.OrderId;

                // Init OrderDetailsDTO
                OrderDetailsDTO orderDetailsDTO = new OrderDetailsDTO();

                // Add to OrderDetailsDTO
                foreach (var item in cart)
                {
                    orderDetailsDTO.OrderId = orderId;
                    orderDetailsDTO.UserId = userId;
                    orderDetailsDTO.ProductId = item.ProductId;
                    orderDetailsDTO.Quantity = item.Quantity;

                    db.OrderDetails.Add(orderDetailsDTO);

                    db.SaveChanges();
                }
            }

            // Email admin
            var client = new SmtpClient("smtp.mailtrap.io", 2525)
            {
                Credentials = new NetworkCredential("ddff23a7e5b8fa", "353efcea5ea1c4"),
                EnableSsl = true
            };
            client.Send("admin@example.com", "admin@example.com", "New Order", "You have a new order. Order number " + orderId);

            // Reset session
            Session["korpa"] = null;
        }

    }
}
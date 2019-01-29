using ProdajaMeda.Models.Data;
using ProdajaMeda.Models.ViewModels.Prodavnica;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Helpers;
using System.Web.Mvc;
using PagedList;
using ProdajaMeda.Areas.Administrator.Models.ViewModels.Prodavnica;

namespace ProdajaMeda.Areas.Administrator.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ProdavnicaController : Controller
    {
        // GET: Administrator/Prodavnica/Kategorije
        public ActionResult Kategorije()
        {
            List<KategorijaVM> kategorijaVMlist;

            using (Db db = new Db())
            {
                kategorijaVMlist = db.Kategorije
                                    .ToArray()
                                    .OrderBy(x => x.Sorting)
                                    .Select(x => new KategorijaVM(x))
                                    .ToList();
            }

            return View(kategorijaVMlist);
        }

        [HttpPost]
        public string DodajNovuKategoriju(string catName)
        {
            string id;

            using (Db db = new Db())
            {
                if (db.Kategorije.Any(x => x.Name == catName))
                    return "titletaken";

                KategorijaDTO dto = new KategorijaDTO();

                dto.Name = catName;
                dto.Slug = catName.Replace(" ", "-").ToLower();
                dto.Sorting = 100;

                db.Kategorije.Add(dto);
                db.SaveChanges();

                id = dto.Id.ToString();
            }

            return id;
        }

        [HttpPost]
        public void ReorderKategorije(int[] id)
        {
            using (Db db = new Db())
            {
                int count = 1;

                KategorijaDTO dto;


                foreach (var catId in id)
                {
                    dto = db.Kategorije.Find(catId);
                    dto.Sorting = count;
                    db.SaveChanges();

                    count++;
                }
            }
        }

        // GET: Administrator/Prodavnica/ObrisiKategoriju

        public ActionResult ObrisiKategoriju(int id)
        {
            using (Db db = new Db())
            {

                KategorijaDTO dto = db.Kategorije.Find(id);

                db.Kategorije.Remove(dto);

                db.SaveChanges();

            }

            return RedirectToAction("Kategorije");
        }

        [HttpPost]
        public string RenameKategoriju(string newCatName, int id)
        {
            using (Db db = new Db())
            {
                if (db.Kategorije.Any(x => x.Name == newCatName))
                    return "titletaken";

                KategorijaDTO dto = db.Kategorije.Find(id);

                dto.Name = newCatName;
                dto.Slug = newCatName.Replace(" ", " - ").ToLower();


                db.SaveChanges();
            }

            return "ok";
        }

        //GET: Administrator/Prodavnica/AddProduct
        [HttpGet]
        public ActionResult AddProduct()
        {
            ProductVM model = new ProductVM();

            using (Db db = new Db())
            {
                model.Kategorije = new SelectList(db.Kategorije.ToList(), "Id", "Name");
            }

            return View(model);
        }

        [HttpPost]
        public ActionResult AddProduct(ProductVM model, HttpPostedFileBase file)
        {
            if (!ModelState.IsValid)
            {
                using (Db db = new Db())
                {
                    model.Kategorije = new SelectList(db.Kategorije.ToList(), "Id", "Name");
                    return View(model);
                }
            }

            using (Db db = new Db())
            {
                if (db.Products.Any(x => x.Name == model.Name))
                {
                    model.Kategorije = new SelectList(db.Kategorije.ToList(), "Id", "Name");
                    ModelState.AddModelError("", "Dati proizvod vec postoji");
                    return View(model);
                }

            }

            int id;

            using (Db db = new Db())
            {
                ProductDTO product = new ProductDTO();

                product.Name = model.Name;
                product.Slug = model.Name.Replace(" ", "-").ToLower();
                product.Description = model.Description;
                product.Price = model.Price;
                product.CategoryId = model.CategoryId;

                KategorijaDTO catDTO = db.Kategorije.FirstOrDefault(x => x.Id == model.CategoryId);
                product.CategoryName = catDTO.Name;

                db.Products.Add(product);
                db.SaveChanges();

                id = product.Id;
            }

            TempData["SM"] = "Ddodali ste proizvod";

            #region Upload Image

            var originalDirectory = new DirectoryInfo(string.Format("{0}Images\\Uploads", Server.MapPath(@"\")));

            var pathString1 = Path.Combine(originalDirectory.ToString(), "Products");
            var pathString2 = Path.Combine(originalDirectory.ToString(), "Products\\" + id.ToString());
            var pathString3 = Path.Combine(originalDirectory.ToString(), "Products\\" + id.ToString() + "\\Thumbs");
            var pathString4 = Path.Combine(originalDirectory.ToString(), "Products\\" + id.ToString() + "\\Gallery");
            var pathString5 = Path.Combine(originalDirectory.ToString(), "Products\\" + id.ToString() + "\\Gallery\\Thumbs");

            if (!Directory.Exists(pathString1))
                Directory.CreateDirectory(pathString1);

            if (!Directory.Exists(pathString2))
                Directory.CreateDirectory(pathString2);

            if (!Directory.Exists(pathString3))
                Directory.CreateDirectory(pathString3);

            if (!Directory.Exists(pathString4))
                Directory.CreateDirectory(pathString4);

            if (!Directory.Exists(pathString5))
                Directory.CreateDirectory(pathString5);

            if (file != null && file.ContentLength > 0)
            {
                string ext = file.ContentType.ToLower();

                if (ext != "image/jpg" &&
                    ext != "image/jpeg" &&
                    ext != "image/pjpeg" &&
                    ext != "image/gif" &&
                    ext != "image/x-png" &&
                    ext != "iamge/png")
                {
                    using (Db db = new Db())
                    {
                        model.Kategorije = new SelectList(db.Kategorije.ToList(), "Id", "Name");
                        ModelState.AddModelError("", "Fotografija nije ubacena - pogresna ekstenzija");
                        return View(model);
                    }
                }

                string imageName = file.FileName;

                using (Db db = new Db())
                {
                    ProductDTO dto = db.Products.Find(id);
                    dto.ImageName = imageName;

                    db.SaveChanges();
                }

                var path = string.Format("{0}\\{1}", pathString2, imageName);
                var path2 = string.Format("{0}\\{1}", pathString3, imageName);

                file.SaveAs(path);

                WebImage img = new WebImage(file.InputStream);
                img.Resize(200, 200);
                img.Save(path2);

            }
            #endregion

            return RedirectToAction("AddProduct");
        }

        public ActionResult Products(int? page, int? catId)
        {
            List<ProductVM> listOfProductVM;
            var pageNumber = page ?? 1;

            using (Db db = new Db())
            {
                listOfProductVM = db.Products.ToArray()
                                .Where(x => catId == null || catId == 0 || x.CategoryId == catId)
                                .Select(x => new ProductVM(x))
                                .ToList();

                ViewBag.Kategorije = new SelectList(db.Kategorije.ToList(), "Id", "Name");

                ViewBag.SelectedCat = catId.ToString();
            }

            var onePageOfProducts = listOfProductVM.ToPagedList(pageNumber, 25);
            ViewBag.OnePageOfProducts = onePageOfProducts;

            return View(listOfProductVM);
        }

        [HttpGet]
        public ActionResult EditProduct(int id)
        {
            ProductVM model;
            using (Db db = new Db())
            {
                ProductDTO dto = db.Products.Find(id);

                if (dto == null)
                {
                    return Content("Dati proizvod ne postoji!");
                }

                model = new ProductVM(dto);

                model.Kategorije = new SelectList(db.Kategorije.ToList(), "Id", "Name");
                model.Galerija = Directory.EnumerateFiles(Server.MapPath("~/Images/Uploads/Products/" + id + "/Gallery/Thumbs"))
                                          .Select(fn => Path.GetFileName(fn));
            }
            return View(model);
        }

        [HttpPost]
        public ActionResult EditProduct(ProductVM model, HttpPostedFileBase file)
        {
            int id = model.Id;

            using (Db db = new Db())
            {
                model.Kategorije = new SelectList(db.Kategorije.ToList(), "Id", "Name");
            }
            model.Galerija = Directory.EnumerateFiles(Server.MapPath("~/Images/Uploads/Products/" + id + "/Gallery/Thumbs"))
                                      .Select(fn => Path.GetFileName(fn));

            if (!ModelState.IsValid)
            {
                return View(model);

            }
            using (Db db = new Db())
            {
                if (db.Products.Where(x => x.Id != id).Any(x => x.Name == model.Name))
                {
                    ModelState.AddModelError("", "Naziv proizvoda vec postoji !");
                }
            }

            //Azuriranje proizvoda
            using (Db db = new Db())
            {
                ProductDTO dto = db.Products.Find(id);

                dto.Name = model.Name;
                dto.Slug = model.Name.Replace(" ", "-").ToLower();
                dto.Description = model.Description;
                dto.Price = model.Price;
                dto.CategoryId = model.CategoryId;
                dto.ImageName = model.ImageName;

                KategorijaDTO catDTO = db.Kategorije.FirstOrDefault(x => x.Id == model.CategoryId);
                dto.CategoryName = catDTO.Name;

                db.SaveChanges();
            }

            TempData["SM"] = "Upravo ste izmenili proizvod";

            #region Image Upload

            //Proveravamo da li je fajl uplodovan

            if (file != null  && file.ContentLength > 0)
            {
                string ext = file.ContentType.ToLower();

                //Verifikacija ekstenzije
                if (ext != "image/jpg" &&
                    ext != "image/jpeg" &&
                    ext != "image/pjpeg" &&
                    ext != "image/gif" &&
                    ext != "image/x-png" &&
                    ext != "iamge/png")
                {
                    using (Db db = new Db())
                    {
                        ModelState.AddModelError("", "Fotografija nije ubacena - pogresna ekstenzija");
                        return View(model);
                    }
                }
                //Setovanje putanje do foldera
                var originalDirectory = new DirectoryInfo(string.Format("{0}Images\\Uploads", Server.MapPath(@"\")));

                var pathString1 = Path.Combine(originalDirectory.ToString(), "Products\\" + id.ToString());
                var pathString2 = Path.Combine(originalDirectory.ToString(), "Products\\" + id.ToString() + "\\Thumbs");

                //Brisanje fajla iz foldera

                DirectoryInfo di1 = new DirectoryInfo(pathString1);
                DirectoryInfo di2 = new DirectoryInfo(pathString2);

                foreach (FileInfo file2 in di1.GetFiles())
                    file2.Delete();
                foreach (FileInfo file3 in di2.GetFiles())
                    file3.Delete();

                //Cuvanje fotografija
                string imageName = file.FileName;
                using (Db db = new Db())
                {
                    ProductDTO dto = db.Products.Find(id);
                    dto.ImageName = imageName;

                    db.SaveChanges();
                }

                var path = string.Format("{0}\\{1}", pathString1, imageName);
                var path2 = string.Format("{0}\\{1}", pathString2, imageName);

                file.SaveAs(path);

                WebImage img = new WebImage(file.InputStream);
                img.Resize(200, 200);
                img.Save(path2);


            }

            #endregion

            return RedirectToAction("EditProduct");
        }

        public ActionResult DeleteProduct(int id)
        {
            //Brisanje proizvoda iz baze
            using (Db db = new Db())
            {
                ProductDTO dto = db.Products.Find(id);
                db.Products.Remove(dto);

                db.SaveChanges();
            }

            //Brisanje foldera
            var originalDirectory = new DirectoryInfo(string.Format("{0}Images\\Uploads", Server.MapPath(@"\")));
            string pathString = Path.Combine(originalDirectory.ToString(), "Products\\" + id.ToString());

            if (Directory.Exists(pathString))
                Directory.Delete(pathString,true);


            return RedirectToAction("Products");
        }

        [HttpPost]
        public void SaveGalleryImages(int id)
        {
            foreach (string fileName in Request.Files)
            {
                
                HttpPostedFileBase file = Request.Files[fileName];

                // Provera da nije null
                if (file != null && file.ContentLength > 0)
                {
                    // Set directory paths
                    var originalDirectory = new DirectoryInfo(string.Format("{0}Images\\Uploads", Server.MapPath(@"\")));

                    string pathString1 = Path.Combine(originalDirectory.ToString(), "Products\\" + id.ToString() + "\\Gallery");
                    string pathString2 = Path.Combine(originalDirectory.ToString(), "Products\\" + id.ToString() + "\\Gallery\\Thumbs");

                    
                    var path = string.Format("{0}\\{1}", pathString1, file.FileName);
                    var path2 = string.Format("{0}\\{1}", pathString2, file.FileName);

                    
                    //Cuvanje originala
                    file.SaveAs(path);
                    WebImage img = new WebImage(file.InputStream);
                    img.Resize(200, 200);
                    img.Save(path2);
                }

            }
        }

        [HttpPost]
        public void DeleteImage(int id, string imageName)
        {
            string fullPath1 = Request.MapPath("~/Images/Uploads/Products/" + id.ToString() + "/Gallery/" + imageName);
            string fullPath2 = Request.MapPath("~/Images/Uploads/Products/" + id.ToString() + "/Gallery/Thumbs/" + imageName);

            if (System.IO.File.Exists(fullPath1))
                System.IO.File.Delete(fullPath1);

            if (System.IO.File.Exists(fullPath2))
                System.IO.File.Delete(fullPath2);
        }

        public ActionResult Orders()
        {
            // Inicijalizacija liste za OrdersForAdminVM
            List<OrdersForAdminVM> ordersForAdmin = new List<OrdersForAdminVM>();

            using (Db db = new Db())
            {
                // Inicijalizacija liste za OrderVM
                List<OrderVM> orders = db.Orders.ToArray().Select(x => new OrderVM(x)).ToList();

                foreach (var order in orders)
                {
                    Dictionary<string, int> productsAndQty = new Dictionary<string, int>();

                    decimal total = 0m;

                    List<OrderDetailsDTO> orderDetailsList = db.OrderDetails.Where(X => X.OrderId == order.OrderId).ToList();

                    UserDTO user = db.Users.Where(x => x.Id == order.UserId).FirstOrDefault();
                    string username = user.Username;

                    foreach (var orderDetails in orderDetailsList)
                    {
                        
                        ProductDTO product = db.Products.Where(x => x.Id == orderDetails.ProductId).FirstOrDefault();

                        decimal price = product.Price;

                        string productName = product.Name;

                        productsAndQty.Add(productName, orderDetails.Quantity);

                        total += orderDetails.Quantity * price;
                    }

                    //Dodavanje u  ordersForAdminVM listu
                    ordersForAdmin.Add(new OrdersForAdminVM()
                    {
                        OrderNumber = order.OrderId,
                        Username = username,
                        Total = total,
                        ProductsAndQty = productsAndQty,
                        CreatedAt = order.CreatedAt
                    });
                }
            }

            return View(ordersForAdmin);
        }




    }
}


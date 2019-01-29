using ProdajaMeda.Models.Data;
using ProdajaMeda.Models.ViewModels.Prodavnica;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace ProdajaMeda.Controllers
{
    public class ProdavnicaController : Controller
    {
        // GET: Prodavnica
        public ActionResult Index()
        {
            return RedirectToAction("Index", "Pages");
        }

        public ActionResult CategoryMenuPartial()
        {
            // Deklaracija liste za kategoriju
            List<KategorijaVM> categoryVMList;

            // inicijalizacija liste
            using (Db db = new Db())
            {
                categoryVMList = db.Kategorije.ToArray().OrderBy(x => x.Sorting).Select(x => new KategorijaVM(x)).ToList();
            }
            return PartialView(categoryVMList);
        }

        public ActionResult Category(string name)
        {
            
            List<ProductVM> productVMList;

            using (Db db = new Db())
            {
               
                KategorijaDTO categoryDTO = db.Kategorije.Where(x => x.Slug == name).FirstOrDefault();
                int catId = categoryDTO.Id;

                
                productVMList = db.Products.ToArray().Where(x => x.CategoryId == catId).Select(x => new ProductVM(x)).ToList();

              
                var productCat = db.Products.Where(x => x.CategoryId == catId).FirstOrDefault();
                ViewBag.CategoryName = productCat.CategoryName;
            }

            return View(productVMList);
        }


        [ActionName("product-details")]
        public ActionResult ProductDetails(string name)
        {          
            ProductVM model;
            ProductDTO dto;

            int id = 0;

            using (Db db = new Db())
            {
                
                if (!db.Products.Any(x => x.Slug.Equals(name)))
                {
                    return RedirectToAction("Index", "Shop");
                }
       
                dto = db.Products.Where(x => x.Slug == name).FirstOrDefault();
                
                id = dto.Id;
                
                model = new ProductVM(dto);
            }
            
            model.Galerija = Directory.EnumerateFiles(Server.MapPath("~/Images/Uploads/Products/" + id + "/Gallery/Thumbs"))
                                                .Select(fn => Path.GetFileName(fn));
            
            return View("ProductDetails", model);
        }


   
    }
}
﻿using System;
using System.IO;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Model;
using Model.Models;

namespace ProductScraper.Services
{
    public class ProductService
    {
        readonly ApplicationContext _context;
        readonly StreamWriter _streamWriter;

        public ProductService(ApplicationContext context, StreamWriter streamWriter) {
            _context = context;
            _streamWriter = streamWriter;
        }

        public void Add(Product product)
        {
            product.IsNew = true;
            product.VeganType = VeganType.Unkown;
            _context.Products.Add(product);

            var workloadItem = new WorkloadItem
            {
                Product = product,
                Message = "Nieuw product gevonden",
                CreatedOn = DateTime.Now
            };
            _context.WorkloadItems.Add(workloadItem);

            _context.SaveChanges();

            _streamWriter.WriteLine($"{product.Id}: Nieuw product {product.Name}");
        }

        public void UpdateOrAdd(Product product)
        {
            var existingProduct = _context.Products.Include("ProductProductCategories.ProductCategory").FirstOrDefault(_ => _.Url == product.Url);
            if (existingProduct == null)
            {
                Add(product);
            }
            else
            {
                //Change product name
                if (existingProduct.Name != product.Name)
                {
                    existingProduct.Name = product.Name;
                }

                //Change ingredients and allergy info
                if (existingProduct.Ingredients != product.Ingredients
                    || existingProduct.AllergyInfo != product.AllergyInfo)
                {
                    existingProduct.Ingredients = product.Ingredients;
                    existingProduct.AllergyInfo = product.AllergyInfo;

                    var workloadItem = new WorkloadItem
                    {
                        Product = existingProduct,
                        Message = "Product heeft aanpassingen",
                        CreatedOn = DateTime.Now
                    };
                    _context.WorkloadItems.Add(workloadItem);
                    _streamWriter.WriteLine($"{product.Id}: Product aangepast {product.Name}");
                }

                //Add new product categorie
                if (existingProduct.ProductCategories.Contains(product.ProductCategories.First()))
                {
                    existingProduct.ProductCategories.Add(product.ProductCategories.First());
                    var workloadItem = new WorkloadItem
                    {
                        Product = existingProduct,
                        Message = "Product heeft nieuwe categorie",
                        CreatedOn = DateTime.Now
                    };
                    _context.WorkloadItems.Add(workloadItem);
                    _streamWriter.WriteLine($"{product.Id}: Product heeft nieuwe categorie {product.Name}");
                }

                _context.SaveChanges();
            }
        }
    }
}

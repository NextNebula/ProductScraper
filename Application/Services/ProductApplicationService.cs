﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Model;
using Model.Models;
using Model.Requests;

namespace Application.Services
{
    public class ProductApplicationService
    {
        readonly IMapper _mapper;
        readonly ApplicationContext _context;
        readonly DateTime _productActivityDate;

        public ProductApplicationService(ApplicationContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
            _productActivityDate = DateTime.Now;
        }

        private void Create(ProductStoreRequest request)
        {
            var product = _mapper.Map<Product>(request);
            product.VeganType = VeganType.Unkown;
            product.ProductCategories.Add(request.ProductCategory);

            _context.Products.Add(product);

            var workloadItemNew = new WorkloadItem
            {
                Message = "Nieuw product gevonden",
                CreatedOn = product.LastScrapeDate
            };
            product.WorkloadItems.Add(workloadItemNew);

            if (product.IsStoreAdvertisedVegan)
            {
                var workloadItemVegan = new WorkloadItem
                {
                    Message = "Product is wel vegan volgens winkel",
                    CreatedOn = request.LastScrapeDate
                };
                product.WorkloadItems.Add(workloadItemVegan);
            }

            _context.SaveChanges();
        }

        public void Update(ProductUpdateRequest request)
        {
            var product = _context.Products
                    .Include(p => p.WorkloadItems)
                    .Single(_ => _.Id == request.Id);

            var oldVeganType = product.VeganType;

            _mapper.Map(request, product);

            if (oldVeganType != product.VeganType)
            {
                product.AddProductActivityVeganTypeChanged(_productActivityDate);
            }

            _context.SaveChanges();
        }

        public void CreateOrUpdate(ProductStoreRequest request)
        {
            var existingProduct = _context.Products
                    .Include(p => p.WorkloadItems)
                    .Include(p => p.ProductProductCategories)
                    .SingleOrDefault(_ => _.Code == request.Code && _.StoreType == request.StoreType);

            ProcessAmmount(request);

            request.Ingredients = request.Ingredients.Replace("\n", " ");

            if (existingProduct != null)
            {
                Update(request, existingProduct);
            }
            else
            {
                Create(request);
            }
        }

        private void Update(ProductStoreRequest request, Product product)
        {
            if (request.Ingredients != product.Ingredients || request.AllergyInfo != product.AllergyInfo)
            {
                var workloadItem = new WorkloadItem
                {
                    Product = product,
                    Message = "Product heeft aanpassingen",
                    CreatedOn = request.LastScrapeDate
                };
                _context.WorkloadItems.Add(workloadItem);
            }

            if (product.IsStoreAdvertisedVegan != request.IsStoreAdvertisedVegan)
            {
                var workloadItem = new WorkloadItem
                {
                    Product = product,
                    Message = $"Product is { (request.IsStoreAdvertisedVegan ? "wel" : "niet")} vegan volgens winkel",
                    CreatedOn = request.LastScrapeDate
                };
                _context.WorkloadItems.Add(workloadItem);
            }

            _mapper.Map(request, product);

            if (!product.ProductCategories.Contains(request.ProductCategory))
            {
                product.ProductCategories.Add(request.ProductCategory);
            }

            _context.SaveChanges();
        }

        public void Delete(int id)
        {
            var product = _context.Products.Find(id);
            _context.Products.Remove(product);
            _context.SaveChanges();
        }

        public void ProcessAll()
        {
            var productIds = _context.Products
                .Select(_ => _.Id)
                .ToList();
            Process(productIds);
        }

        public void ProcessWorkload()
        {
            var productIds = _context.Products
                .Include(p => p.WorkloadItems)
                .Where(_ => _.WorkloadItems.Any(w => !w.IsProcessed))
                .Select(_ => _.Id)
                .ToList();
            Process(productIds);
        }

        private void Process(IList<int> productIds)
        {
            var ingredients = _context.Ingredients.ToList();

            foreach (var productId in productIds)
            {
                var product = _context.Products
                    .Include(p => p.WorkloadItems)
                    .Include(p => p.ProductActivities)
                    .Include("ProductIngredients.Ingredient")
                    .First(p => p.Id == productId);

                SetMatchedIngredients(product, ingredients);
                SetVeganType(product);

                if (product.IsProcessed)
                {
                    var workloadItems = product.WorkloadItems.Where(_ => !_.IsProcessed);
                    foreach (var workloadItem in workloadItems)
                    {
                        workloadItem.IsProcessed = true;
                    }
                }

                _context.SaveChanges();
            }
        }

        public Product ProcessVeganType(int productId)
        {
            var product = _context.Products
                    .Include("ProductIngredients.Ingredient")
                    .Single(p => p.Id == productId);
            var oldVeganType = product.VeganType;
            var ingredients = _context.Ingredients.ToList();

            SetMatchedIngredients(product, ingredients);

            if (product.IsStoreAdvertisedVegan)
            {
                product.VeganType = VeganType.Vegan;
            }
            else if (product.MatchedIngredients.Any(_ => _.VeganType == VeganType.Not))
            {
                product.VeganType = VeganType.Not;
            }
            else if (product.MatchedIngredients.Any(_ => _.VeganType == VeganType.Unsure))
            {
                product.VeganType = VeganType.Unsure;
            }
            else
            {
                product.VeganType = VeganType.Vegan;
            }

            if (oldVeganType != product.VeganType)
            {
                product.AddProductActivityVeganTypeChanged(_productActivityDate);
            }

            product.IsProcessed = true;

            _context.SaveChanges();
            return product;
        }

        private void SetMatchedIngredients(Product product, List<Ingredient> ingredients)
        {
            var foundIngredients = new List<Ingredient>();
            foreach (var ingredient in ingredients)
            {
                var foundMatch = false;

                foundMatch = DetectAllergyKeyword(ingredient, product);

                if (!foundMatch)
                {
                    foundMatch = DetectKeyword(ingredient, product);
                }

                if (foundMatch)
                {
                    foundIngredients.Add(ingredient);
                }
            }

            foreach(var foundIngredient in foundIngredients)
            {
                if (!product.MatchedIngredients.Contains(foundIngredient))
                {
                    product.MatchedIngredients.Add(foundIngredient);
                    product.ProductActivities.Add(new ProductActivity
                    {
                        Type = ProductActivityType.IngredientAdded,
                        Detail = foundIngredient.Name,
                        CreatedOn = _productActivityDate
                    });
                }
            }

            var outdatedIngredients = new List<Ingredient>();
            foreach (var matchedIngredient in product.MatchedIngredients)
            {
                if (!foundIngredients.Contains(matchedIngredient))
                {
                    outdatedIngredients.Add(matchedIngredient);
                }
            }

            foreach (var outdatedIngredient in outdatedIngredients)
            {
                product.MatchedIngredients.Remove(outdatedIngredient);
                product.ProductActivities.Add(new ProductActivity
                {
                    Type = ProductActivityType.IngredientRemoved,
                    Detail = outdatedIngredient.Name,
                    CreatedOn = _productActivityDate
                });
            }
        }

        private void SetVeganType(Product product)
        {
            var oldVeganType = product.VeganType;

            if (product.IsStoreAdvertisedVegan || product.IsManufacturerAdvertisedVegan)
            {
                product.VeganType = VeganType.Vegan;
                product.IsProcessed = true;
            }
            else if (string.IsNullOrWhiteSpace(product.Ingredients))
            {
                product.VeganType = VeganType.Unkown;
            }
            else if (product.MatchedIngredients.Any(_ => _.VeganType == VeganType.Not))
            {
                product.VeganType = VeganType.Not;
                if (!product.MatchedIngredients.Where(_ => _.VeganType == VeganType.Not).All(_ => _.NeedsReview))
                {
                    product.IsProcessed = true;
                }
            }
            else if (product.MatchedIngredients.Any(_ => _.VeganType == VeganType.Unsure))
            {
                product.VeganType = VeganType.Unsure;
                if (!product.MatchedIngredients.Where(_ => _.VeganType == VeganType.Unsure).All(_ => _.NeedsReview))
                {
                    product.IsProcessed = true;
                }
            }

            if (oldVeganType != product.VeganType)
            {
                product.AddProductActivityVeganTypeChanged(_productActivityDate);
            }
        }

        public void RemoveOutdatedProducts(StoreType storeType, DateTime scrapeDate)
        {
            var outdatedProducts = _context.Products
                .Include(p => p.WorkloadItems)
                .Where(_ => _.StoreType == storeType && _.LastScrapeDate < scrapeDate.AddDays(-7));

            foreach (var outdatedProduct in outdatedProducts)
            {
                if (!_context.WorkloadItems.Any(_ => _.Product.Id == outdatedProduct.Id && _.Message == "Product niet gevonden"))
                {
                    var workloadItem = new WorkloadItem
                    {
                        Product = outdatedProduct,
                        Message = "Product niet gevonden",
                        CreatedOn = scrapeDate
                    };
                    _context.WorkloadItems.Add(workloadItem);
                }
            }

            _context.SaveChanges();
        }

        public void DeleteProductActivity(int productActivityId)
        {
            var productActivity = _context.ProductActivities.Find(productActivityId);
            _context.ProductActivities.Remove(productActivity);
            _context.SaveChanges();
        }

        public void DeleteWorkloadItem(int workloadItemId)
        {
            var workloadItem = _context.WorkloadItems.Find(workloadItemId);
            _context.WorkloadItems.Remove(workloadItem);
            _context.SaveChanges();
        }

        private bool DetectAllergyKeyword(Ingredient ingredient, Product product)
        {
            foreach (var keyWord in ingredient.AllergyKeywords)
            {
                var match = Regex.Match(" " + product.AllergyInfo + " ", @"[\s\W]" + keyWord + @"[\s\W]");
                if (match.Success)
                {
                    return true;
                }
            }

            return false;
        }

        private bool DetectKeyword(Ingredient ingredient, Product product)
        {
            var ingredients = product.Ingredients;
            foreach (var ignoreKeyword in ingredient.IgnoreKeywords)
            {
                ingredients = ingredients.Replace(ignoreKeyword, "");
            }

            foreach (var keyWord in ingredient.Keywords)
            {

                var match = Regex.Match(" " + ingredients + " ", @"[\s\W]" + keyWord + @"[\s\W]");
                if (match.Success)
                {
                    return true;
                }
            }

            return false;
        }

        private void ProcessAmmount(ProductStoreRequest productStoreRequest)
        {
            var name = productStoreRequest.Name;
            var ammount = productStoreRequest.Ammount;

            var piecesNameMatch = Regex.Match(name, @"\d+ stuks", RegexOptions.IgnoreCase);

            if (piecesNameMatch.Success)
            {
                ammount = piecesNameMatch.Value;
            }

            var piecesWithAmmountNameMatch = Regex.Match(name, @"\d+\sx\s[\d.,\s]+[gmkl][lg]*", RegexOptions.IgnoreCase);
            if (piecesWithAmmountNameMatch.Success)
            {
                ammount = piecesWithAmmountNameMatch.Value;
            }

            //Convert liter (lower then 1l) to milliliter
            if (Regex.Match(ammount, @"0[.,]\d+\sl", RegexOptions.IgnoreCase).Success)
            {
                var liter = float.Parse(Regex.Match(ammount, @"(0[.,]\d+)").Groups[0].Value);
                ammount = $"{liter * 1000} ml";
            }

            //Use correct unit symbols
            ammount = ammount.ToLower();
            ammount = ammount.Replace("gram", "g").Replace("gr", "g");
            ammount = ammount.Replace(". ", ",").Replace(", ", ",");
            ammount = ammount.Replace(" g", "g").Replace(" kg", "kg").Replace(" ml", "ml").Replace(" l", "l");

            //Remove ammount info from name
            name = Regex.Replace(name, @"\d+\sx\s[\d.,\s]+[gmkl][lg]*", string.Empty, RegexOptions.IgnoreCase);
            name = Regex.Replace(name, @"\d+ stuks", string.Empty, RegexOptions.IgnoreCase);
            name = Regex.Replace(name, @"[\d.,]+[gmkl][lg]*", string.Empty, RegexOptions.IgnoreCase);

            productStoreRequest.Name = name.Trim();
            productStoreRequest.Ammount = ammount.Trim();
        }
    }
}

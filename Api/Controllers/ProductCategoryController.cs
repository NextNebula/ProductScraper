﻿using System.Linq;
using Api.Models;
using Api.Resources;
using Application.Services;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Model;

namespace Api.Controllers
{
    public class ProductCategoryController : Controller
    {
        public static readonly string RouteName = nameof(ProductCategoryController).Replace("Controller", "");
        readonly IMapper _mapper;
        readonly ApplicationContext _context;
        readonly ProductCategoryApplicationService _productCategoryService;

        public ProductCategoryController(ApplicationContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
            _productCategoryService = new ProductCategoryApplicationService(_context, _mapper);
        }

        public IActionResult Index()
        {
            var productCategories = _context.ProductCategories.ToList();
            var viewModel = productCategories.Select(_ => new ProductCategoryViewModel().Map(_, _mapper));

            return View(nameof(Index), viewModel);
        }

        public IActionResult Create()
        {
            return View(nameof(Create), new ProductCategoryCreateViewModel());
        }

        [HttpPost]
        public IActionResult Create(ProductCategoryCreateViewModel viewmodel)
        {
            var productCategory = _productCategoryService.Create(viewmodel.Request);
            return RedirectToAction(nameof(Update), new { id = productCategory.Id });
        }

        public IActionResult Update(int id)
        {
            var productCategory = _context.ProductCategories.Include(_ => _.StoreCategories).First(_ => _.Id == id);
            var viewModel =  ProductCategoryUpdateViewModel.Map(productCategory, _mapper);

            return View(nameof(Update), viewModel);
        }

        [HttpPost]
        public IActionResult Update(ProductCategoryUpdateViewModel viewModel)
        {
            _productCategoryService.Update(viewModel.Request);
            return Update(viewModel.Request.Id);
        }

        [HttpPost]
        public IActionResult Delete(int id)
        {
            var deleteResult = _productCategoryService.Delete(id);

            if (!deleteResult)
            {
                ModelState.AddModelError(string.Empty, CommonTerms.Error_ProductCategory_In_Use);
                return Update(id);
            }

            return Redirect(nameof(Index));
        }
    }
}

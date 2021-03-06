﻿using System;
using System.Web;
using System.Web.Mvc;
using MvcContrib;
using MvcContrib.Filters;
using SharpArch.Domain.Commands;
using SharpArch.Domain.PersistenceSupport;
using SharpArch.NHibernate.Web.Mvc;
using SharpArchitecture.MultiTenant.ApplicationServices.Commands;
using SharpArchitecture.MultiTenant.Core;
using SharpArchitecture.MultiTenant.Web.Controllers.Customers.Queries;
using SharpArchitecture.MultiTenant.Web.Controllers.Customers.ViewModels;

namespace SharpArchitecture.MultiTenant.Web.Controllers.Customers
{
  public class CustomersController : Controller
  {
    private const int DefaultPageSize = 20;
    private readonly ICommandProcessor _commandProcessor;
    private readonly ICustomerListQuery _customerListQuery;
    private readonly IRepository<Customer> _customerRepository;

    public CustomersController(ICommandProcessor commandProcessor, ICustomerListQuery customerListQuery, IRepository<Customer> customerRepository)
    {
      _commandProcessor = commandProcessor;
      _customerListQuery = customerListQuery;
      _customerRepository = customerRepository;
    }

    [HttpGet]
    [Transaction]
    public ActionResult Index(int? page)
    {
      var customers = _customerListQuery.GetPagedList(page ?? 1, DefaultPageSize);
      var viewModel = new CustomerListViewModel { Customers = customers };
      return View(viewModel);
    }

    [HttpGet]
    [ModelStateToTempData]
    [Transaction]
    public ActionResult Create()
    {
      var viewModel = TempData.SafeGet<CustomerFormViewModel>() ?? new CustomerFormViewModel();
      return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [ModelStateToTempData]
    [Transaction]
    public ActionResult Create(CustomerFormViewModel viewModel)
    {
      if (!ViewData.ModelState.IsValid) {
        TempData.SafeAdd(viewModel);
        return this.RedirectToAction(c => c.Create());
      }

      try {
        var customer = new Customer { Code = viewModel.Code, Name = viewModel.Name };
        _customerRepository.SaveOrUpdate(customer);
        TempData[ControllerEnums.GlobalViewDataProperty.PageMessage.ToString()] = string.Format("Successfully created customer '{0}'", customer.Name);
      }
      catch (Exception ex) {
        TempData[ControllerEnums.GlobalViewDataProperty.PageMessage.ToString()] = string.Format("An error occurred creating the customer: {0}", ex.Message);
        return this.RedirectToAction(c => c.Create());
      }

      return this.RedirectToAction(c => c.Index(null));
    }

    [HttpGet]
    [ModelStateToTempData]
    [Transaction]
    public ActionResult Edit(int id)
    {
      var customer = _customerRepository.Get(id);
      var viewModel = new CustomerFormViewModel(customer);
      return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [ModelStateToTempData]
    [Transaction]
    public ActionResult Edit(CustomerFormViewModel viewModel)
    {
      if (!ViewData.ModelState.IsValid) {
        return this.RedirectToAction(c => c.Edit(viewModel.Id));
      }

      try {
        var customer = _customerRepository.Get(viewModel.Id);
        customer.Code = viewModel.Code;
        customer.Name = viewModel.Name;

        _customerRepository.SaveOrUpdate(customer);
        TempData[ControllerEnums.GlobalViewDataProperty.PageMessage.ToString()] = string.Format("Successfully updated customer '{0}'", customer.Name);
      }
      catch (Exception ex) {
        TempData[ControllerEnums.GlobalViewDataProperty.PageMessage.ToString()] = string.Format("An error occurred updating the customer: {0}", ex.Message);
        return this.RedirectToAction(c => c.Edit(viewModel.Id));
      }

      return this.RedirectToAction(c => c.Index(null));
    }

    [HttpGet]
    [ModelStateToTempData]
    [Transaction]
    public ActionResult Delete(int id)
    {
      var customer = _customerRepository.Get(id);
      var viewModel = new CustomerFormViewModel(customer);
      return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [ModelStateToTempData]
    [Transaction]
    public ActionResult Delete(CustomerFormViewModel viewModel)
    {
      if (!ViewData.ModelState.IsValid) {
        return this.RedirectToAction(c => c.Edit(viewModel.Id));
      }

      try {
        var customer = _customerRepository.Get(viewModel.Id);
        _customerRepository.Delete(customer);
        TempData[ControllerEnums.GlobalViewDataProperty.PageMessage.ToString()] = string.Format("Successfully deleted customer '{0}'", customer.Name);
      }
      catch (Exception ex) {
        TempData[ControllerEnums.GlobalViewDataProperty.PageMessage.ToString()] = string.Format("An error occurred deleting the customer: {0}", ex.Message);
        return this.RedirectToAction(c => c.Edit(viewModel.Id));
      }

      return this.RedirectToAction(c => c.Index(null));
    }

    [HttpGet]
    public ActionResult Import()
    {
      var viewModel = new ImportCustomersFormViewModel();
      return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Transaction]
    public ActionResult Import(ImportCustomersFormViewModel viewModel)
    {
      if (!ViewData.ModelState.IsValid) {
        return View(viewModel);
      }

      var command = new ImportCustomersCommand(viewModel.UploadKey);
      var results = _commandProcessor.Process(command);

      if (results.Success) {
        return RedirectToAction("Index");
      }

      return View(viewModel);
    }

    // [HttpPost]
    [Transaction]
    public ActionResult Upload(Guid uploadKey, HttpPostedFileBase fileData)
    {
      var command = new UploadFileCommand(uploadKey, fileData, ControllerContext.HttpContext.User.Identity.Name);
      var results = _commandProcessor.Process(command);
      return Json(new { Status = results.Success ? "OK" : "Error" }); 
    }
  }
}
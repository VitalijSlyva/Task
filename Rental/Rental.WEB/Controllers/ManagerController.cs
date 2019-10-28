﻿using Microsoft.AspNet.Identity;
using Rental.BLL.DTO.Identity;
using Rental.BLL.DTO.Log;
using Rental.BLL.DTO.Rent;
using Rental.BLL.Interfaces;
using Rental.WEB.Attributes;
using Rental.WEB.Interfaces;
using Rental.WEB.Models.Domain_Models.Identity;
using Rental.WEB.Models.Domain_Models.Rent;
using Rental.WEB.Models.View_Models.Manager;
using Rental.WEB.Models.View_Models.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Rental.WEB.Controllers
{
    /// <summary>
    /// Manager actions.
    /// </summary>
    [ExceptionLogger]
    [Authorize(Roles = "manager")]
    [AuthorizeWithoutBann]
    public class ManagerController : Controller
    {
        private IManagerService _managerService;

        private IRentMapperDM _rentMapperDM;

        private IIdentityMapperDM _identityMapperDM;

        private ILogWriter _logWriter;

        /// <summary>
        /// Create services and mappers for work.
        /// </summary>
        /// <param name="managerService">Manager service</param>
        /// <param name="rentMapper">Rent mapper</param>
        /// <param name="log">Log service</param>
        /// <param name="identityMapperDM">Identity mapper</param>
        public ManagerController(IManagerService managerService, IRentMapperDM rentMapper, 
            ILogWriter log,IIdentityMapperDM identityMapperDM)
        {
            _managerService = managerService;
            _rentMapperDM = rentMapper;
            _logWriter = log;
            _identityMapperDM = identityMapperDM;
        }

        /// <summary>
        /// Get order by id for confirm.
        /// </summary>
        /// <param name="id">Order id</param>
        /// <returns>View</returns>
        public ActionResult Confirm(int? id)
        {
            if (id != null)
            {
                var orderDTO = _managerService.GetOrder(id.Value,true);
                if (orderDTO == null)
                    return View("CustomNotFound", "_Layout", "Заказ не найден");

                var order = _rentMapperDM.ToOrderDM.Map<OrderDTO, OrderDM>(orderDTO);
                ConfirmDM confirm = new ConfirmDM()
                {
                    Order = order
                };
                confirm.Order.Profile = _identityMapperDM.ToProfileDM.Map<ProfileDTO, ProfileDM>(orderDTO.Profile);
                return View(confirm);
            }

            return View("CustomNotFound", "_Layout", "Заказ не найден");
        }

        /// <summary>
        /// Confirm order.
        /// </summary>
        /// <param name="confirmDM">Confirm object</param>
        /// <returns>View</returns>
        [HttpPost]
        public ActionResult Confirm(ConfirmDM confirmDM)
        {
            if (!confirmDM.IsConfirmed&&String.IsNullOrEmpty(confirmDM.Description))
            {
                ModelState.AddModelError("", "Не указана причина отклонения");
            }

            if (ModelState.IsValid)
            {
                ConfirmDTO confirm = _rentMapperDM.ToConfirmDTO.Map<ConfirmDM, ConfirmDTO>(confirmDM);
                confirm.User = new User()
                {
                    Id = User.Identity.GetUserId()
                };
                _managerService.ConfirmOrder(confirm);
                _logWriter.CreateLog("Подтвердил заказ"+confirm.Order.Id, User.Identity.GetUserId());

                return RedirectToAction("ShowConfirms", "Manager", null);
            }

            var orderDTO = _managerService.GetOrder(confirmDM.Order.Id,true);
            confirmDM.Order = _rentMapperDM.ToOrderDM.Map<OrderDTO, OrderDM>(orderDTO);
            confirmDM.Order.Profile = _identityMapperDM.ToProfileDM.Map<ProfileDTO, ProfileDM>(orderDTO.Profile);

            return View(confirmDM);
        }

        /// <summary>
        /// Show orders for confirm.
        /// </summary>
        /// <param name="model">View model</param>
        /// <param name="sortMode">Sort mode</param>
        /// <param name="page">Page number</param>
        /// <param name="selectedMode">Selected sort mode</param>
        /// <returns>View</returns>
        public ActionResult ShowConfirms(ShowConfirmsVM model, int sortMode = 0, int page = 1, int selectedMode = 1)
        {
            if (sortMode == 0)
            {
                sortMode = selectedMode;
            }

            var orders = _managerService.GetForConfirms();
            var ordersDM = _rentMapperDM.ToOrderDM.Map<IEnumerable<OrderDTO>, List<OrderDM>>(orders);
            var filters = new List<Models.View_Models.Shared.Filter>();

            if(ordersDM!=null && ordersDM.Count > 0)
            {
                if (model.Filters == null || model.Filters.Count == 0)
                {
                    void CreateFilters(string name, Func<OrderDM, string> value)
                    {
                        filters.AddRange(ordersDM.Select(x => value(x)).Distinct()
                            .Select(x => new Models.View_Models.Shared.Filter()
                            {
                                Name = name,
                                Text = x,
                                Checked = false
                            }));
                    }
                    CreateFilters("Автомобиль", x => x.Car.Brand.Name+" "+x.Car.Model);
                    CreateFilters("Статус", x => x.Payment.IsPaid ? "Оплачен" : "Неоплачен");}
                else
                {
                    filters = model.Filters;
                    void FilterTest(string name, Func<OrderDM, string> value)
                    {
                        if (ordersDM.Count > 0 && model.Filters.Any(f => f.Name == name && f.Checked))
                        {
                            ordersDM = ordersDM.Where(p => model.Filters
                            .Any(f => f.Name == name && f.Text == value(p) && f.Checked)).ToList();
                        }
                    }

                    FilterTest("Автомобиль", x => x.Car.Brand.Name + " " + x.Car.Model);
                    FilterTest("Статус", x => x.Payment.IsPaid ? "Оплачен" : "Неоплачен");
                }
            }

            var sortModes = new List<string>();
            sortModes.Add("По номеру");
            sortModes.Add("По номеру");
            sortModes.Add("По статусу");
            sortModes.Add("По статусу");
            sortModes.Add("По атомобилю");
            sortModes.Add("По автомобилю");
            switch (sortMode)
            {
                case 1:
                    ordersDM = ordersDM.OrderBy(x => x.Id).ToList();
                    break;
                case 2:
                    ordersDM = ordersDM.OrderByDescending(x => x.Id).ToList();
                    break;
                case 3:
                    ordersDM = ordersDM.OrderBy(x => x.Payment.IsPaid).ToList();
                    break;
                case 4:
                    ordersDM = ordersDM.OrderByDescending(x => x.Payment.IsPaid).ToList();
                    break;
                case 5:
                    ordersDM = ordersDM.OrderBy(x => x.Car.Brand.Name+" "+x.Car.Model).ToList();
                    break;
                case 6:
                    ordersDM = ordersDM.OrderByDescending(x => x.Car.Brand.Name + " " + x.Car.Model).ToList();
                    break;
            }

            int pageSize = 7;
            int count = ordersDM.Count;
            ordersDM = ordersDM.Skip((page - 1) * pageSize).Take(pageSize).ToList();
            PageInfo pageInfo = new PageInfo { PageNumber = page, PageSize = pageSize, TotalItem = count };
            if ((page < 1&& count != 0) || (page > pageInfo.TotalPages && count != 0) 
                || sortMode < 1 || sortMode > sortModes.Count)
            {
                 return  View("CustomNotFound", "_Layout", "Страница не найдена");
            }

            ShowConfirmsVM confirmsVM = new ShowConfirmsVM() {
                Orders = ordersDM,
                Filters = filters,
                PageInfo=pageInfo,
                SortModes = sortModes,
                SelectedMode = sortMode
            };

            return View("ShowConfirms", confirmsVM);
        }

        /// <summary>
        /// Show orders for return.
        /// </summary>
        /// <param name="model">View model</param>
        /// <param name="sortMode">Sort mode</param>
        /// <param name="page">Page number</param>
        /// <param name="selectedMode">Selected sort mode</param>
        /// <returns>View</returns>
        public ActionResult ShowReturns(ShowReturnsVM model, int sortMode = 0, int page = 1, int selectedMode = 1)
        {
            if (sortMode == 0)
            {
                sortMode = selectedMode;
            }

            var orders = _managerService.GetForReturns();
            var ordersDM = _rentMapperDM.ToOrderDM.Map<IEnumerable<OrderDTO>, List<OrderDM>>(orders);
            var filters = new List<Models.View_Models.Shared.Filter>();

            if (ordersDM != null && ordersDM.Count > 0)
            {
                if (model.Filters == null || model.Filters.Count == 0)
                {
                    void CreateFilters(string name, Func<OrderDM, string> value)
                    {
                        filters.AddRange(ordersDM.Select(x => value(x)).Distinct()
                            .Select(x => new Models.View_Models.Shared.Filter()
                            {
                                Name = name,
                                Text = x,
                                Checked = false
                            }));
                    }
                    CreateFilters("Автомобиль", x => x.Car.Brand.Name + " " + x.Car.Model);
                }
                else
                {
                    filters = model.Filters;
                    void FilterTest(string name, Func<OrderDM, string> value)
                    {
                        if (ordersDM.Count > 0 && model.Filters.Any(f => f.Name == name && f.Checked))
                        {
                            ordersDM = ordersDM.Where(p => model.Filters
                            .Any(f => f.Name == name && f.Text == value(p) && f.Checked)).ToList();
                        }
                    }

                    FilterTest("Автомобиль", x => x.Car.Brand.Name + " " + x.Car.Model);
                }
            }

            var sortModes = new List<string>();
            sortModes.Add("По номеру");
            sortModes.Add("По номеру");
            sortModes.Add("По атомобилю");
            sortModes.Add("По автомобилю");
            switch (sortMode)
            {
                case 1:
                    ordersDM = ordersDM.OrderBy(x => x.Id).ToList();
                    break;
                case 2:
                    ordersDM = ordersDM.OrderByDescending(x => x.Id).ToList();
                    break;
                case 3:
                    ordersDM = ordersDM.OrderBy(x => x.Car.Brand.Name + " " + x.Car.Model).ToList();
                    break;
                case 4:
                    ordersDM = ordersDM.OrderByDescending(x => x.Car.Brand.Name + " " + x.Car.Model).ToList();
                    break;
            }

            int pageSize = 7;
            int count = ordersDM.Count;
            ordersDM = ordersDM.Skip((page - 1) * pageSize).Take(pageSize).ToList();
            PageInfo pageInfo = new PageInfo { PageNumber = page, PageSize = pageSize, TotalItem = count };
            if ((page < 1&& count != 0) || (page > pageInfo.TotalPages && count != 0) 
                || sortMode < 1 || sortMode > sortModes.Count)
            {
                 return  View("CustomNotFound", "_Layout", "Страница не найдена");
            }

            ShowReturnsVM returnsVM = new ShowReturnsVM() {
                Orders = ordersDM,
                Filters =filters,
                PageInfo =pageInfo,
                SortModes = sortModes,
                SelectedMode = sortMode
            };

            return View("ShowReturns", returnsVM);
        }

        /// <summary>
        /// Show order by id for return.
        /// </summary>
        /// <param name="id">Order id</param>
        /// <returns>View</returns>
        public ActionResult Return(int? id)
        {
            if (id != null)
            {
                var orderDTO = _managerService.GetOrder(id.Value, false);
                if (orderDTO == null)
                    return View("CustomNotFound", "_Layout", "Заказ не найден");

                ReturnDM returnDM = new ReturnDM() { Order = _rentMapperDM.ToOrderDM.Map<OrderDTO, OrderDM>(orderDTO) };
                return View(returnDM);
            }

            return View("CustomNotFound", "_Layout", "Заказ не найден");
        }

        /// <summary>
        /// Return order.
        /// </summary>
        /// <param name="returnDM">Return object</param>
        /// <param name="withCrash">Return with crash</param>
        /// <returns>View</returns>
        [HttpPost]
        public ActionResult Return(ReturnDM returnDM, bool withCrash = false)
        {
            if (withCrash && (returnDM.Crash == null || String.IsNullOrEmpty(returnDM.Crash.Description)
                || returnDM.Crash.Payment == null || returnDM.Crash.Payment.Price <= 0))
            {
                ModelState.AddModelError("", "Не все поля заполнены корректно");
            }
            else
            {
                returnDM.IsReturned = true;
                ReturnDTO returnDTO = _rentMapperDM.ToReturnDTO.Map<ReturnDM, ReturnDTO>(returnDM);
                returnDTO.User = new User() { Id = User.Identity.GetUserId() };
                if (withCrash == false)
                    returnDTO.Crash = null;
                _managerService.ReturnCar(returnDTO);
                _logWriter.CreateLog("Отклонил заказ" + returnDTO.Order.Id, User.Identity.GetUserId());

                return RedirectToAction("ShowReturns", "Manager", null);
            }

            var orderDTO = _managerService.GetOrder(returnDM.Order.Id, false);
            returnDM = new ReturnDM()
            {
                Order = _rentMapperDM.ToOrderDM.Map<OrderDTO, OrderDM>(orderDTO)
            };

            return View(returnDM);
        }
    }
}
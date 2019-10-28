﻿using Rental.BLL.Abstracts;
using Rental.BLL.DTO.Rent;
using Rental.BLL.Interfaces;
using Rental.DAL.Entities.Rent;
using Rental.DAL.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Rental.BLL.Services
{
    /// <summary>
    /// Service for rent actions.
    /// </summary>
    public class RentService : Service, IRentService
    {
        /// <summary>
        /// Create units and mappers for work.
        /// </summary>
        /// <param name="mapperDTO">Mapper for converting database entities to DTO entities</param>
        /// <param name="rentUnit">Rent unit of work</param>
        /// <param name="identityUnit">Udentity unit of work</param>
        /// <param name="identityMapper">Mapper for converting identity entities to BLL classes</param>
        /// <param name="log">Service for logging</param>
        public RentService(IRentMapperDTO mapperDTO, IRentUnitOfWork rentUnit,
                 IIdentityUnitOfWork identityUnit, IIdentityMapperDTO identityMapper, ILogService log)
                   : base(mapperDTO, rentUnit, identityUnit, identityMapper, log)
        {

        }

        /// <summary>
        /// Get car by id.
        /// </summary>
        /// <param name="id">Car id</param>
        /// <returns>Car</returns>
        public CarDTO GetCar(int? id)
        {
            try
            {
                if (id == null)
                    return null;
                var car = RentUnitOfWork.Cars.Get(id.Value);
                if (car == null)
                    return null;

                return RentMapperDTO.ToCarDTO.Map<Car, CarDTO>(car);
            }
            catch (Exception e)
            {
                CreateLog(e, "RentService", "GetCar");

                return null;
            }
        }

        /// <summary>
        /// Get all cars.
        /// </summary>
        /// <returns>Cars</returns>
        public IEnumerable<CarDTO> GetCars()
        {
            try
            {
                var cars = RentUnitOfWork.Cars.Show();

                return RentMapperDTO.ToCarDTO.Map<IEnumerable<Car>, List<CarDTO>>(cars);
            }
            catch (Exception e)
            {
                CreateLog(e, "RentService", "GetCars");

                return null;
            }
        }

        /// <summary>
        /// Get free days for car.
        /// </summary>
        /// <param name="carId">Id</param>
        /// <returns>Days</returns>
        public Dictionary<DateTime, bool> GetFreeDates(int carId)
        {
            try
            {
                if (GetCar(carId) == null)
                    return null;
                var result = new Dictionary<DateTime, bool>();
                for (int i = 0; i < 30; i++)
                {
                    var date = DateTime.Now.AddDays(i).Date;
                    result.Add(date, !RentUnitOfWork.Orders.Show()
                        .Any(x => x.CarId == carId && (x.DateStart.Date <= date && x.DateEnd.Date >= date)
                        && (x?.Confirm?.FirstOrDefault()?.IsConfirmed ?? true) != false&&
                        (x?.Return?.FirstOrDefault()?.IsReturned??false)==false));
                }
                return result;
            }
            catch (Exception e)
            {
                CreateLog(e, "RentService", "GetCar");

                return null;
            }
        }
    }
}

﻿using Rental.DAL.Entities.Identity;
using System;

namespace Rental.DAL.Interfaces
{
    /// <summary>
    /// Interface for working with profile entity.
    /// </summary>
    public interface IClientManager : ICreator<Profile>,IUpdateer<Profile> 
    {
    }
}

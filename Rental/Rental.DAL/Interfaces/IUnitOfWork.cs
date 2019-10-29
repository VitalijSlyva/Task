﻿using System;

namespace Rental.DAL.Interfaces
{
    /// <summary>
    /// Common intreface of unit for wotk.
    /// </summary>
    public interface IUnitOfWork
    {
        IIdentityUnitOfWork Identity { get; }

        IRentUnitOfWork Rent { get; }
    }
}

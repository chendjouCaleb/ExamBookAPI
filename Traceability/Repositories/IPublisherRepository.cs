﻿using System.Collections.Generic;
using System.Threading.Tasks;
using Traceability.Models;

namespace Traceability.Repositories
{
    public interface IPublisherRepository
    {
        Publisher? GetById(string id);
        Task<Publisher?> GetByIdAsync(string id);
        
        
        ICollection<Publisher> GetById(ICollection<string> id);
        Task<ICollection<Publisher>> GetByIdAsync(ICollection<string> id);

        Task SaveAsync(Publisher publisher);
        Task SaveAllAsync(ICollection<Publisher> publishers);

        Task UpdateAsync(Publisher publisher);

        Task Delete(Publisher publisher);
    }
}
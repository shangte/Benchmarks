﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Benchmarks.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.Extensions.Options;

namespace Benchmarks.Data
{
    public class EfDb : IDb
    {
        private readonly IRandom _random;
        private readonly ApplicationDbContext _dbContext;

        public EfDb(IRandom random, ApplicationDbContext dbContext, IOptions<AppSettings> appSettings)
        {
            _random = random;
            _dbContext = dbContext;
        }

        private static readonly Func<ApplicationDbContext, int, Task<World>> _firstWorldQuery
            = EF.CompileAsyncQuery((ApplicationDbContext context, int id)
                => context.World.First(w => w.Id == id));

        public Task<World> LoadSingleQueryRow()
        {
            var id = _random.Next(1, 10001);

            return _dbContext.World.FirstAsync(w => w.Id == id);
        }

        public async Task<World[]> LoadMultipleQueriesRows(int count)
        {
            var result = new World[count];

            for (var i = 0; i < count; i++)
            {
                var id = _random.Next(1, 10001);

                result[i] = await _firstWorldQuery(_dbContext, id);
            }

            return result;
        }

        private static readonly Func<ApplicationDbContext, int, Task<World>> _firstWorldTrackedQuery
            = EF.CompileAsyncQuery((ApplicationDbContext context, int id)
                => context.World.AsTracking().First(w => w.Id == id));

        public async Task<World[]> LoadMultipleUpdatesRows(int count)
        {
            var results = new World[count];

            for (var i = 0; i < count; i++)
            {
                var id = _random.Next(1, 10001);
                var result = await _dbContext.World.AsTracking().FirstAsync(w => w.Id == id);

                _dbContext.Entry(result).Property("RandomNumber").CurrentValue = _random.Next(1, 10001);
                results[i] = result;                
            }

            await _dbContext.SaveChangesAsync();

            return results;
        }

#if NETCOREAPP2_1 || NETCOREAPP2_2
        private static readonly Func<ApplicationDbContext, AsyncEnumerable<Fortune>> _fortunesQuery
            = EF.CompileAsyncQuery((ApplicationDbContext context) => context.Fortune);
#else
        private static readonly Func<ApplicationDbContext, IAsyncEnumerable<Fortune>> _fortunesQuery
            = EF.CompileAsyncQuery((ApplicationDbContext context) => context.Fortune);
#endif
        public async Task<IEnumerable<Fortune>> LoadFortunesRows()
        {
            var result = await _dbContext.Fortune.ToListAsync();

            result.Add(new Fortune { Message = "Additional fortune added at request time." });
            result.Sort();

            return result;
        }
    }

#if !NETCOREAPP2_1 && !NETCOREAPP2_2
    static class EntityFrameworkQueryableExtensions
    {
        public static async Task<List<TSource>> ToListAsync<TSource>(
            this IAsyncEnumerable<TSource> source, CancellationToken cancellationToken = default)
        {
            var list = new List<TSource>();
            await foreach (var element in source.WithCancellation(cancellationToken))
            {
                list.Add(element);
            }

            return list;
        }
    }
#endif
}

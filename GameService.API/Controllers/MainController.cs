using System;
using System.Collections.Generic;
using System.Linq;
using GameService.Domain.Models;
using GameService.Domain.Repositories;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Driver;

namespace GameApp.Controllers
{
    [ApiController]
    [Route("/v1a/")]
    public class MainController : Controller
    {

        private readonly MongoRepository _mongoRepository;

        public MainController(MongoRepository mongoRepository)
        {
            _mongoRepository = mongoRepository;
        }

        // GET
        public IActionResult Index()
        {
            return Ok(
                _mongoRepository
                    .GetListSorted(x => true, 
                        new BsonDocumentSortDefinition<Game>(new BsonDocument("$natural", -1)))
                    .ToList()
            );
        }
        
        
        [HttpPost("create")]
        public IActionResult Create([FromBody]Game game)
        {
            var existingGame = _mongoRepository.GetOne(game.Code);
            if (existingGame != null) return Conflict();

            try
            {
                _mongoRepository.CreateOne(game);
            }
            catch (MongoWriteException e)
            {
                return BadRequest(e);
            }
            
            //TODO: set messages to frontend to start
            
            return Ok();
        }
    }
}
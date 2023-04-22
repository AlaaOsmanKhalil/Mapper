﻿using AutoMapper;
using CoreCodeCamp.Data;
using CoreCodeCamp.Models;
using Grpc.Core;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.IdentityModel.Tokens;
using Microsoft.VisualBasic;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace CoreCodeCamp.Controllers
{
    [Route("api/[controller]")]
    [ApiVersion("2.0")]
    [ApiController]
    public class Camps2Controller : ControllerBase
    { 
        private readonly ICampRepository _repository;
        private readonly IMapper _mapper;
        private readonly LinkGenerator _linkGenerator;

        public Camps2Controller(ICampRepository campRepository, IMapper mapper, LinkGenerator linkGenerator)
        {
            _repository= campRepository;
            _mapper=mapper;
            _linkGenerator=linkGenerator;


        }

        [HttpGet]
        public async Task<IActionResult> Get(bool includeTalks = false) 
        { 
            try
            {
                var results = await _repository.GetAllCampsAsync(includeTalks);
                var result = new
                {
                    Count = results.Count(),
                    results = _mapper.Map<CampModel[]>(results)
                };
                return Ok(result);
            }

            catch(Exception)
            {
                return this.StatusCode(500, "Database Failure");
            }
        }

        [HttpGet("{moniker}")]
        public async Task<ActionResult<CampModel>> Get(string moniker)
        {
            try
            {
                var result =await _repository.GetCampAsync(moniker);
                if (result == null)
                {
                    return NotFound();
                }
                 return _mapper.Map<CampModel>(result);
            }
            catch(Exception)
            {
                return this.StatusCode(500, "Database Failure");
            }
        }

        [HttpGet("search")]
        public async Task<ActionResult<CampModel[]>> SearchByDate(DateTime theDate, bool includeTalks = false)
        {
            try
            {
                var results = await _repository.GetAllCampsByEventDate(theDate, includeTalks);
               
                if (!results.Any())
                {
                    return NotFound();
                }

                return _mapper.Map<CampModel[]>(results);
            }
            catch (Exception)
            {
                return this.StatusCode(500, "Database Failure");
            }
        }

        public async Task<ActionResult<CampModel>> Post(CampModel model)
        {
            try
            {
                var existing = await _repository.GetCampAsync(model.Moniker);

                if (existing != null)
                {
                    return BadRequest("Moniker in use");    
                }

                var location = _linkGenerator.GetPathByAction("Get", "Camps",
                    new { moniker = model.Moniker });

                if (string.IsNullOrWhiteSpace(location))
                {
                    return BadRequest("Couldn't use current moniker");
                }

                var camp = _mapper.Map<Camp>(model);
                _repository.Add(camp);

                if(await _repository.SaveChangesAsync())
                {
                    return Created($"/api/camps/{camp.Moniker}", _mapper.Map<CampModel>(camp));
                }

                return Ok();
            }
            catch (Exception)
            {
                return this.StatusCode(500, "Database Failure");
            }
            return BadRequest();
        }

        [HttpPut("{moniker}")]
        public async Task<ActionResult<CampModel>> Put(string moniker, CampModel model)
        {
            try
            {
                var oldCamp = await _repository.GetCampAsync(moniker);
                if(oldCamp == null)
                {
                    return NotFound($"Couldn't find any camp with this moniker {moniker}");
                }
                _mapper.Map(model, oldCamp);

                if (await _repository.SaveChangesAsync())
                {
                    return _mapper.Map<CampModel>(oldCamp);
                }
            }

            catch (Exception)
            {
                return this.StatusCode(500, "Database Failure");
            }
            return BadRequest();
        }

        [HttpDelete("{moniker}")]
        public async Task<IActionResult> Delete (string moniker)
        {
            try
            {
                var camp = await _repository.GetCampAsync(moniker);
                if (camp == null)
                {
                    return NotFound($"Couldn't find any camp with this moniker {moniker}");
                }
                _repository.Delete(camp);
                if (await _repository.SaveChangesAsync())
                {
                    return Ok();
                }
            }
            catch (Exception)
            {
                return this.StatusCode(500, "Database Failure");
            }
            return BadRequest("Failed to delete");
        }
    }
}

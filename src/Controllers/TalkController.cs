using AutoMapper;
using CoreCodeCamp.Data;
using CoreCodeCamp.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Identity.Client;
using System;
using System.Threading.Tasks;

namespace CoreCodeCamp.Controllers
{
    [ApiController]
    [Route("api/camps/{moniker}/talks")]
    public class TalkController : ControllerBase
    {
        private readonly ICampRepository _repository;
        private readonly IMapper _mapper;
        private readonly LinkGenerator _linkGenerator;

        public TalkController(ICampRepository campRepository, IMapper mapper, LinkGenerator linkGenerator)
        {
            _repository = campRepository;
            _mapper = mapper; 
            _linkGenerator = linkGenerator;
        }

        [HttpGet]
        public async Task<ActionResult<TalkModel[]>> Get(string moniker)
        {
            try
            {
                var talks = await _repository.GetTalksByMonikerAsync(moniker,true);
                return _mapper.Map<TalkModel[]>(talks);

            }
            catch (Exception)
            {
                return this.StatusCode(500, "Database Failure");
            }
        }
        [HttpGet("{talkId:int}")]
        public async Task<ActionResult<TalkModel>> Get(string moniker, int talkId)
        {
            try
            {
                var talk = await _repository.GetTalkByMonikerAsync(moniker, talkId,true);
                return _mapper.Map<TalkModel>(talk);
            }
            catch (Exception)
            {

                return this.StatusCode(500, "Database Failure");
            }
        }
        [HttpPost]
        public async Task<ActionResult<TalkModel>> Post(string moniker, TalkModel model)
        {
            try
            {
                var camp = await _repository.GetCampAsync(moniker);
                if (camp == null) return BadRequest($"Camp {camp} doesn't exist");

                var talk = _mapper.Map<Talk>(model);
                talk.Camp = camp;

                if (model.Speaker == null) return BadRequest("SpeakerId is required");
                var speaker = await _repository.GetSpeakerAsync(model.Speaker.SpeakerId);

                if (speaker == null) return BadRequest("Speaker is nit found");
                talk.Speaker = speaker;
                _repository.Add(talk);

                if (await _repository.SaveChangesAsync())
                {
                    var url = _linkGenerator.GetPathByAction(HttpContext,
                    "Get",
                    values: new { moniker, id = talk.TalkId });
                    return Created(url, _mapper.Map<TalkModel>(talk));
                }
                else
                {
                    return BadRequest("Failed to save new Talk");
                }
            }

            catch (Exception)
            {

                return this.StatusCode(500, "Database Failure");
            }
        }

        [HttpPut("{id:int}")]
        public async Task<ActionResult<TalkModel>> Get(string moniker, int id, TalkModel model)
        {
            try
            {
                var talk = await _repository.GetTalkByMonikerAsync(moniker, id, true);
                if (talk == null) return NotFound("Couldn't find any talk");

                _mapper.Map(model, talk);

                if (model.Speaker != null)
                {
                    var speaker = await _repository.GetSpeakerAsync(model.Speaker.SpeakerId);
                    if(speaker != null)
                    {
                        talk.Speaker = speaker;
                    }
                }

                if (await _repository.SaveChangesAsync())
                {
                    return _mapper.Map<TalkModel>(talk);
                }
                else { return BadRequest("Failde to update the database"); }
            }
            catch (Exception)
            {

                return this.StatusCode(500, "Database Failure");
            }
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult>Delete(int id, string moniker)
        {
            try
            {
                var talk = await _repository.GetTalkByMonikerAsync(moniker,id);
                if (talk == null)
                {
                    return NotFound($"Couldn't find any talk with this moniker {moniker}");
                }
                _repository.Delete(talk);

                if (await _repository.SaveChangesAsync())
                {
                    return Ok();
                }
                else return BadRequest();
            }
            catch (Exception)
            {

                return this.StatusCode(500, "Database Failure");
            }
        }
    }
}

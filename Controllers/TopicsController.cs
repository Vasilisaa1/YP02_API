using CodeQuest.Context;
using Microsoft.AspNetCore.Mvc;

namespace CodeQuest.Controllers
{
    [Route("api/Topics")]
    public class TopicsController : Controller
    {
        /// <summary>
        /// Получение списка тем
        /// </summary>
        [Route("List")]
        [HttpGet]
        [ApiExplorerSettings(GroupName = "v1")]
        [ProducesResponseType(typeof(List<Model.Topics>), 200)]
        [ProducesResponseType(500)]
        public ActionResult List()
        {
            try
            {
                IEnumerable<Model.Topics> topics = new TopicsContext().Topics;
                return Json(topics);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        /// <summary>
        /// Получение темы по ID
        /// </summary>
        [Route("Item")]
        [HttpGet]
        [ApiExplorerSettings(GroupName = "v1")]
        [ProducesResponseType(typeof(Model.Topics), 200)]
        [ProducesResponseType(500)]
        public ActionResult Item(int id)
        {
            try
            {
                Model.Topics topic = new TopicsContext().Topics.FirstOrDefault(x => x.id == id);
                if (topic == null)
                    return NotFound($"Тема с ID {id} не найдена");

                return Json(topic);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        /// <summary>
        /// Добавление новой темы
        /// </summary>
        [Route("Add")]
        [HttpPost]
        [ApiExplorerSettings(GroupName = "v2")]
        [ProducesResponseType(200)]
        [ProducesResponseType(500)]
        public ActionResult Add([FromForm] Model.Topics topic)
        {
            try
            {
                TopicsContext context = new TopicsContext();
                context.Topics.Add(topic);
                context.SaveChanges();
                return Ok("Тема успешно добавлена");
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        /// <summary>
        /// Обновление данных темы
        /// </summary>
        [Route("Update")]
        [HttpPut]
        [ApiExplorerSettings(GroupName = "v3")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public ActionResult Update(int id, [FromForm] Model.Topics topic)
        {
            try
            {
                using (TopicsContext context = new TopicsContext())
                {
                    var existing = context.Topics.FirstOrDefault(x => x.id == id);
                    if (existing == null)
                        return NotFound($"Тема с ID {id} не найдена");

                    existing.title = topic.title;
                    existing.short_description = topic.short_description;
                    existing.full_description = topic.full_description;
                    existing.order_index = topic.order_index;

                    context.SaveChanges();
                    return Ok("Тема успешно обновлена");
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        /// <summary>
        /// Удаление темы по ID
        /// </summary>
        [Route("DeleteById")]
        [HttpDelete]
        [ApiExplorerSettings(GroupName = "v4")]
        [ProducesResponseType(200)]
        [ProducesResponseType(500)]
        public ActionResult DeleteById(int id)
        {
            try
            {
                TopicsContext context = new TopicsContext();
                var topic = context.Topics.FirstOrDefault(x => x.id == id);
                if (topic == null)
                    return NotFound($"Тема с ID {id} не найдена");

                context.Topics.Remove(topic);
                context.SaveChanges();
                return Ok("Тема удалена");
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        /// <summary>
        /// Очистка всех тем
        /// </summary>
        [Route("ClearAll")]
        [HttpDelete]
        [ApiExplorerSettings(GroupName = "v4")]
        [ProducesResponseType(200)]
        [ProducesResponseType(500)]
        public ActionResult ClearAll()
        {
            try
            {
                TopicsContext context = new TopicsContext();
                context.Topics.RemoveRange(context.Topics);
                context.SaveChanges();
                return Ok("Все темы удалены");
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }
    }
}

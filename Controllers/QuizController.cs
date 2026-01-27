using CodeQuest.Context;
using Microsoft.AspNetCore.Mvc;

namespace CodeQuest.Controllers
{

    [ApiController]
    [Route("api/Quiz")]
    public class QuizController : Controller
    {
        [Route("List")]
        [HttpGet]
        [ApiExplorerSettings(GroupName = "v1")]
        public ActionResult List()
        {
            try
            {
                var quizzes = new QuizContext().Quiz.ToList();
                return Json(quizzes);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [Route("Item")]
        [HttpGet]
        [ApiExplorerSettings(GroupName = "v1")]
        public ActionResult Item(int id)
        {
            try
            {
                var quiz = new QuizContext().Quiz.FirstOrDefault(x => x.id == id);
                return quiz != null ? Json(quiz) : NotFound();
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [Route("Add")]
        [HttpPost]
        [ApiExplorerSettings(GroupName = "v2")]
        public ActionResult Add([FromForm] Model.Quiz quiz)
        {
            try
            {
                Console.WriteLine("=== API: ADD QUIZ START ===");
                Console.WriteLine($"Received Topic ID: {quiz?.topic_id}");
                Console.WriteLine($"Received Question: {quiz?.question_text}");
                Console.WriteLine($"Received Options: {quiz?.options}");

                // Если options не в JSON формате, конвертируем его
                if (!string.IsNullOrEmpty(quiz.options) && !quiz.options.Trim().StartsWith("["))
                {
                    Console.WriteLine("Converting options to JSON format...");

                    var optionsArray = quiz.options.Split(';')
                        .Where(opt => !string.IsNullOrWhiteSpace(opt))
                        .Select(opt => opt.Trim())
                        .ToArray();

                    quiz.options = System.Text.Json.JsonSerializer.Serialize(optionsArray);
                    Console.WriteLine($"Converted Options to JSON: {quiz.options}");
                }

                using var context = new QuizContext();

                // Проверяем существование темы через Topics
                var topicExists = context.Topics.Any(t => t.id == quiz.topic_id);
                Console.WriteLine($"Topic exists check: {topicExists}");

                if (!topicExists)
                {
                    // Получаем список существующих тем для отладки
                    var existingTopics = context.Topics.Select(t => t.id).ToList();
                    Console.WriteLine($"Existing topic IDs: {string.Join(", ", existingTopics)}");

                    return NotFound($"Topic with ID {quiz.topic_id} not found. Existing topics: {string.Join(", ", existingTopics)}");
                }

                // Сохраняем вопрос
                context.Quiz.Add(quiz);
                context.SaveChanges();

                Console.WriteLine($"=== API: QUIZ SAVED SUCCESSFULLY, ID: {quiz.id} ===");
                return Ok($"Вопрос успешно добавлен с ID: {quiz.id}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"=== API: ERROR ===");
                Console.WriteLine($"Error Type: {ex.GetType().Name}");
                Console.WriteLine($"Error Message: {ex.Message}");

                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner Exception: {ex.InnerException.Message}");
                }

                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
        [Route("Update")]
        [HttpPut]
        [ApiExplorerSettings(GroupName = "v3")]
        public ActionResult Update(int id, [FromForm] Model.Quiz quiz)
        {
            try
            {
                Console.WriteLine("=== API: UPDATE QUIZ START ===");
                Console.WriteLine($"Updating Quiz ID: {id}");
                Console.WriteLine($"Received Topic ID: {quiz?.topic_id}");
                Console.WriteLine($"Received Question: {quiz?.question_text}");
                Console.WriteLine($"Received Options: {quiz?.options}");
                Console.WriteLine($"Received Correct Answer: {quiz?.correct_answer}");

                using var context = new QuizContext();
                var existing = context.Quiz.FirstOrDefault(x => x.id == id);
                if (existing == null)
                {
                    Console.WriteLine($"Quiz with ID {id} not found");
                    return NotFound($"Quiz with ID {id} not found");
                }

                // Если options не в JSON формате, конвертируем его (как в методе Add)
                if (!string.IsNullOrEmpty(quiz.options) && !quiz.options.Trim().StartsWith("["))
                {
                    Console.WriteLine("Converting options to JSON format...");
                    var optionsArray = quiz.options.Split(';')
                        .Where(opt => !string.IsNullOrWhiteSpace(opt))
                        .Select(opt => opt.Trim())
                        .ToArray();
                    quiz.options = System.Text.Json.JsonSerializer.Serialize(optionsArray);
                    Console.WriteLine($"Converted Options to JSON: {quiz.options}");
                }

                // Проверяем существование темы
                var topicExists = context.Topics.Any(t => t.id == quiz.topic_id);
                if (!topicExists)
                {
                    var existingTopics = context.Topics.Select(t => t.id).ToList();
                    Console.WriteLine($"Topic with ID {quiz.topic_id} not found. Existing topics: {string.Join(", ", existingTopics)}");
                    return NotFound($"Topic with ID {quiz.topic_id} not found");
                }

                // Обновляем поля
                existing.topic_id = quiz.topic_id;
                existing.question_text = quiz.question_text;
                existing.options = quiz.options;
                existing.correct_answer = quiz.correct_answer;

                context.SaveChanges();

                Console.WriteLine($"=== API: QUIZ UPDATED SUCCESSFULLY ===");
                return Ok("Quiz updated successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"=== API: UPDATE ERROR ===");
                Console.WriteLine($"Error Type: {ex.GetType().Name}");
                Console.WriteLine($"Error Message: {ex.Message}");

                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner Exception: {ex.InnerException.Message}");
                }

                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [Route("DeleteById")]
        [HttpDelete]
        [ApiExplorerSettings(GroupName = "v4")]
        public ActionResult DeleteById(int id)
        {
            try
            {
                var context = new QuizContext();
                var quiz = context.Quiz.First(x => x.id == id);
                context.Quiz.Remove(quiz);
                context.SaveChanges();
                return Ok();
            }
            catch
            {
                return StatusCode(500);
            }
        }

        [Route("ClearAll")]
        [HttpDelete]
        [ApiExplorerSettings(GroupName = "v4")]
        public ActionResult ClearAll()
        {
            try
            {
                var context = new QuizContext();
                context.Quiz.RemoveRange(context.Quiz);
                context.SaveChanges();
                return Ok();
            }
            catch
            {
                return StatusCode(500);
            }
        }

        [Route("ByTopic")]
        [HttpGet]
        [ApiExplorerSettings(GroupName = "v1")]
        public ActionResult ByTopic(int topicId)
        {
            try
            {
                var quizzes = new QuizContext().Quiz
                    .Where(x => x.topic_id == topicId)
                    .ToList();
                return Json(quizzes);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }
    }
}

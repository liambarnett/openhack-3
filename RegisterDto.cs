using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SimpleEchoBot
{
    public class RegisterDto
    {
        public string TeamId { get; set; }
        public List<TeamMemberDto> Members { get; set; }
    }

    public class TeamMemberDto
    {
        public string Id { get; set; }
        public string Name { get; set; }
    }

    public class QuestionDto
    {
        public int Id { get; set; }
        public string Text { get; set; }
        public IEnumerable<QuestionOptionsDto> QuestionOptions { get; set; }
    }

    public class QuestionOptionsDto
    {
        public int Id { get; set; }
        public string Text { get; set; }
    }

    [Serializable]
    public class QuestionChoicesDto
    {
        public int Id { get; set; }
        public string Value { get; set; }
        public int QuestionId { get; set; }

        public override string ToString()
        {
            return Value?.ToString();
        }
    }


    public class AnswerResultDto
    {
        public bool Correct { get; set; }
        public string achievementBadge { get; set; }
        public string achievementBadgeIcon { get; set; }
    }
}
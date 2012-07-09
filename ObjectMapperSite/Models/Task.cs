namespace ObjectMapperSite.Models
{
    using System;

    using ObjectMapper;

    [DbTable(Name = "Tasks")]
    public class Task
    {
        [DbKey, DbIdentity]
        public int Id { get; set; }
        public string Name { get; set; }
        public bool IsDone { get; set; }
        public string Description { get; set; }
        public DateTime DueDate { get; set; }
    }
}
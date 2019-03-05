﻿namespace BLL.DTO
{
    public class PersonDTO
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public string Name { get; set; }
        public string Role { get; set; }
        public string Email { get; set; }
        public virtual TeamDTO Team { get; set; }
    }
}

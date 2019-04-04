﻿using AutoMapper;
using BLL.Configurations;
using BLL.DTO;
using BLL.Exceptions;
using BLL.Interfaces;
using DAL.Interfaces;
using DAL.Entities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BLL.Services
{
    public class TaskService : ITaskService
    {
        private IUnitOfWork db { get; set; }
        private IMapper mapper { get; set; }

        public TaskService(IUnitOfWork uow)
        {
            db = uow;
            mapper = MapperConfig.GetMapperResult();
        }

        #region Main CRUD-operations for Tasks.
        // Finished!!!
        public void DeleteTask(int taskId, string currentUserName)
        {
            TaskDTO task = mapper.Map<TaskInfo, TaskDTO>(db.Tasks.GetById(taskId));

            if (task == null)
                throw new TaskNotFoundException("Task with this id not found");

            PersonDTO managerDTO = mapper.Map<Person, PersonDTO>(db.People.Find(m => m.UserName == currentUserName).SingleOrDefault());

            if (task.AuthorId == managerDTO.Id)
            {
                db.Tasks.Delete(task.Id);
                db.Save();
            }
            else
            {
                throw new TaskAccessException("Access error. You cannot delete this task");
            }
        }
        // Finished!!!
        public void CreateTask(EditTaskDTO task, string authorName, int assigneeId, string priority)
        {
            var newTask = EditTask(task, authorName, assigneeId, priority);

            db.Tasks.Create(newTask);
            db.Save();
        }
        // Finished!!!
        public void UpdateTask(EditTaskDTO task, int id, string authorName, int assigneeId, string priority)
        {
            TaskInfo taskForEdit = db.Tasks.GetById(id);

            if (taskForEdit != null)
            {
                var newTask = EditTask(task, authorName, assigneeId, priority);

                newTask.Id = taskForEdit.Id;
                newTask.Progress = taskForEdit.Progress;
                newTask.StartDate = taskForEdit.StartDate;
                newTask.StatusId = taskForEdit.StatusId;
                newTask.FinishDate = taskForEdit.FinishDate;

                db.Tasks.Update(taskForEdit.Id, newTask);
                db.Save();
            }
        }
        // Finished!!!
        public TaskDTO GetTask(int id)
        {
            var condition = db.Tasks.Find(t => t.Id == id);
            var task = GetTasksWithCondition(condition).FirstOrDefault();

            return task;
        }
        // Finished!!!
        public IEnumerable<TaskDTO> GetAllTasks()
        {
            var condition = db.Tasks.GetAll();
            var allTasks = GetTasksWithCondition(condition);
            
            return allTasks;
        }
        // Finished!!!
        public IEnumerable<TaskDTO> GetTasksOfTeam(string managerId)
        {
            var manager = db.People.Find(p => p.UserId == managerId).SingleOrDefault();
            if (manager == null)
                throw new ManagerNotFoundException("Manager is not found");

            var condition = db.Tasks.Find(t => t.AuthorId == manager.Id);
            var tasks = GetTasksWithCondition(condition);

            return tasks;
        }

        public IEnumerable<TaskDTO> GetInactiveTasks(int teamId)
        {
            var manager = db.Teams.GetById(teamId);
            var tasks = db.Tasks.Find(t => ((t.Author.Id == manager.Id) && ((t.Deadline < DateTime.Now) || (t.Deadline == null))));

            return mapper.Map<IEnumerable<TaskInfo>, IEnumerable<TaskDTO>>(tasks);
        }

        public IEnumerable<TaskDTO> GetCompletedTasks(int teamId)
        {
            var manager = db.Teams.GetById(teamId);
            var tasks = db.Tasks.Find(t => ((t.Author.Id == manager.Id))
                                        && (t.Status.Name == "Completed"));

            return mapper.Map<IEnumerable<TaskInfo>, IEnumerable<TaskDTO>>(tasks);
        }

        public IEnumerable<TaskDTO> GetTasksOfAssignee(string id)
        {
            IEnumerable<TaskInfo> tasks = db.Tasks.Find(t => (t.Assignee.UserId == id))
                                               .OrderByDescending(t => t.Progress);
            IEnumerable<TaskDTO> result = mapper.Map<IEnumerable<TaskInfo>, IEnumerable<TaskDTO>>(tasks);

            return result;
        }

        public IEnumerable<TaskDTO> GetTasksOfAuthor(string id)
        {
            IEnumerable<TaskInfo> tasks = db.Tasks.Find(t => (t.Author.UserId == id))
                                               .OrderByDescending(t => t.Progress);
            IEnumerable<TaskDTO> result = mapper.Map<IEnumerable<TaskInfo>, IEnumerable<TaskDTO>>(tasks);

            return result;
        }

        #endregion

        public void UpdateStatus(int taskId, string statusName, int changerId)
        {
            if (string.IsNullOrWhiteSpace(statusName))
                throw new StatusNotFoundException("Name of status is null or empty");

            Status status = db.Statuses.Find(s => (s.Name == statusName)).SingleOrDefault();

            TaskInfo task = db.Tasks.GetById(taskId);
            if (task == null)
                throw new TaskNotFoundException("Task wasn't found");

            if (task.AuthorId == changerId)
            {
                if ((task.Status.Name == "Executed") && (statusName == "Completed"))
                {
                    task.Progress = 100;
                    task.FinishDate = DateTime.Now;
                }
                else if (statusName == "Canceled")
                    task.Progress = 0;
                else
                    throw new StatuskAccessException("This is status cannot belong to Author");
            }
            else if (task.AssigneeId == changerId)
            {
                switch (statusName)
                {
                    case "Not started":
                        {
                            task.StartDate = null;
                            task.Progress = 0;
                            break;
                        }
                    case "In Progress":
                        {
                            task.StartDate = DateTime.Now;
                            task.Progress = 20;
                            break;
                        }
                    case "Test":
                        {
                            task.Progress = 40;
                            break;
                        }
                    case "Almost Ready":
                        {
                            task.Progress = 60;
                            break;
                        }
                    case "Executed":
                        {
                            task.Progress = 80;
                            break;
                        }
                    default:
                        throw new StatuskAccessException("This status cannot belongs to Author");
                }
            }
            else
                throw new StatuskAccessException("Current person cannot change a status");
            task.Status = status ?? throw new StatusNotFoundException("Status with this name was not found");

            db.Tasks.Update(task.Id, task);
            db.Save();
        }

        public int GetProgressOfTeam(string managerId)
        {
            var tasksOfTeam = GetTasksOfTeam(managerId);

            int sumProgress = 0;
            int counter = 0;

            foreach (var task in tasksOfTeam)
            {
                if (task.Progress.HasValue)
                    sumProgress += task.Progress.Value;

                counter++;
            }

            sumProgress = (counter != 0) ? (sumProgress / counter) : 0;

            return sumProgress;
        }

        public int GetProgressOfAllTasks()
        {
            var tasks = GetAllTasks();

            int sumProgress = 0;
            int counter = 0;

            foreach (var task in tasks)
            {
                if (task.Progress.HasValue)
                    sumProgress += task.Progress.Value;

                counter++;
            }

            sumProgress = (counter != 0) ? (sumProgress / counter) : 0;

            return sumProgress;
        }

        private TaskInfo EditTask(EditTaskDTO task, string authorName, int assigneeId, string priority)
        {
            PersonDTO authorDTO = mapper.Map<Person, PersonDTO>(db.People.Find(p => p.UserName == authorName).SingleOrDefault());
            if (authorDTO == null)
                throw new PersonNotFoundException("Author has not found");

            PersonDTO assigneeDTO = mapper.Map<Person, PersonDTO>(db.People.Find(p => p.Id == assigneeId).SingleOrDefault());
            if (assigneeDTO == null)
                throw new PersonNotFoundException("Assignee has not found");

            PriorityDTO priorityDTO = mapper.Map<Priority, PriorityDTO>(db.Priorities.Find(p => p.Name == priority).SingleOrDefault());
            if (priorityDTO == null)
                throw new PriorityNotFoundException("Priority has not known");

            StatusDTO status = mapper.Map<Status, StatusDTO>(db.Statuses.Find(s => (s.Name == "Not Started")).SingleOrDefault());
            if (status == null)
                throw new StatusNotFoundException("Status \"Not Started\" has not found in database");

            var newTask = new TaskInfo
            {
                Name = task.Name,
                Description = task.Description,
                PriorityId = priorityDTO.Id,
                AuthorId = authorDTO.Id,
                AssigneeId = assigneeDTO.Id,
                StatusId = status.Id,
                Progress = 0,
                StartDate = null,
                FinishDate = null,
                Deadline = task.Deadline,
            };

            return newTask;
        }

        private IEnumerable<TaskDTO> GetTasksWithCondition(IEnumerable<TaskInfo> condition)
        {
            IEnumerable<TaskDTO> tasks = condition
                            .Join(db.People.GetAll(), x => x.AssigneeId, y => y.Id, (x, y) => new { x, Assignee = y.FName + " " + y.LName })
                            .Join(db.People.GetAll(), x => x.x.AuthorId, y => y.Id, (x, y) => new { x.x, x.Assignee, Author = y.FName + " " + y.LName })
                            .Join(db.Statuses.GetAll(), x => x.x.StatusId, y => y.Id, (x, y) => new { x.x, x.Assignee, x.Author, Status = y.Name })
                            .Join(db.Priorities.GetAll(), x => x.x.PriorityId, y => y.Id, (x, y) => new { x.x, x.Assignee, x.Author, x.Status, Priority = y.Name })
                            .Select(n => new TaskDTO
                            {
                                Assignee = n.Assignee,
                                Author = n.Author,
                                Status = n.Status,
                                Priority = n.Priority,
                                Id = n.x.Id,
                                Deadline = n.x.Deadline,
                                Description = n.x.Description,
                                FinishDate = n.x.FinishDate,
                                Name = n.x.Name,
                                Progress = n.x.Progress,
                                StartDate = n.x.StartDate,
                                AssigneeId = n.x.AssigneeId
                            });

            return tasks;
        }
    }
}

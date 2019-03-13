﻿using System;
using System.Collections.Generic;
using BLL.DTO;
using DAL.Entities;

namespace BLL.Interfaces
{
    /// <summary>
    /// Service for working with teams.
    /// </summary>
    public interface ITeamService
    {
        /// <summary>
        /// Get all teams that are contained in database.
        /// </summary>
        /// <returns>Enumeration of teams in type TeamBLL</returns>
        IEnumerable<TeamDTO> GetAllTeams();

        /// <summary>
        /// Change name of team.
        /// </summary>
        /// <param name="id">Id of team</param>
        /// <param name="NewName">New name for team</param>
        /// ///<exception cref="ArgumentException">Team wasn't found</exception>
        void ChangeTeamName(int id, string newName);

        void CreateTeam(Person manager, string teamName);
    }
}

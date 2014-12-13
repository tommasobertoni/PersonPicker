using PersonPicker.Model;
using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PersonPicker.DataAccess
{
    public class SQLitePickDb : SQLiteConnection
    {
        public SQLitePickDb()
            : base("pick.db")
        {
            CreateTable<PersonEntity>();
            CreateTable<PickEntity>();
        }

        #region People Repository
        public List<PersonEntity> GetAllPeople()
        {
            return Table<PersonEntity>().ToList();
        }

        public PersonEntity GetPersonById(int id)
        {
            return (from p in Table<PersonEntity>()
                        where p.Id == id
                        select p).FirstOrDefault();
        }

        public int InsertNewPerson(PersonEntity person)
        {
            return Insert(person);
        }

        public bool UpdatePerson(PersonEntity person)
        {
            return Update(person) > 0;
        }

        public bool DeletePersonById(int id)
        {
            return Delete<PersonEntity>(id) > 0;
        }
        #endregion

        #region Pick Repository
        public List<PickEntity> GetAllPicks()
        {
            return Table<PickEntity>().OrderByDescending(pick => pick.PickTime).ToList();
        }

        public PickEntity GetPickById(int id)
        {
            return (from p in Table<PickEntity>()
                    where p.Id == id
                    select p).FirstOrDefault();
        }

        public int InsertNewPick(PickEntity pick)
        {
            return Insert(pick);
        }

        public bool DeleteAllPicks()
        {
            return DeleteAll<PickEntity>() > 0;
        }
        #endregion

        #region Tools
        public int CountPickForPerson(int personId)
        {
            return GetAllPicks().Count(pick => pick.PersonId == personId);
        }

        #endregion
    }
}

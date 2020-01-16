﻿using System.Collections.Generic;

namespace OnePlayer.Data
{
    public interface IGenreAccessor
    {
        Genre Get(int id);

        Genre Find(string genreName);

        IList<Genre> GetAll();

        Genre Add(Genre genre);

        Genre Update(Genre genre);
    }
}

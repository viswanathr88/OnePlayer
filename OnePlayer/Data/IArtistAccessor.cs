﻿using System.Collections.Generic;

namespace OnePlayer.Data
{
    public interface IArtistAccessor
    {
        Artist Get(int id);

        Artist Find(string artistName);

        IList<Artist> GetAll();

        Artist Add(Artist artist);

        Artist Update(Artist artist);
    }
}

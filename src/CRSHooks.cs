using MoreSlugcats;
using System;
using CustomRegions.Collectables;
using System.Reflection;
using System.Linq;
using System.Collections.Generic;

namespace MagicasContentPack
{
	internal class CRSHooks
	{
		internal static void SaveCustomBroadcast(Player self, ChatlogData.ChatlogID chatlogID)
		{
			DeathPersistentSaveData deathPersistentSaveData = self.room.game.GetStorySession.saveState.deathPersistentSaveData;
			deathPersistentSaveData.chatlogsRead.Remove(self.chatlogID);
			deathPersistentSaveData.prePebChatlogsRead.Remove(self.chatlogID);

			foreach (Broadcasts.BroadcastSaveData broadcastSaveData in deathPersistentSaveData.CustomBroadcastData())
			{
				if (broadcastSaveData.room == self.room.abstractRoom.name && broadcastSaveData.id == self.chatlogID)
				{
					chatlogID = new ChatlogData.ChatlogID(broadcastSaveData.ToString(), false);
				}
			}
			if (chatlogID != null && !deathPersistentSaveData.chatlogsRead.Contains(chatlogID))
			{
				deathPersistentSaveData.chatlogsRead.Add(chatlogID);
			}
		}
	}
}
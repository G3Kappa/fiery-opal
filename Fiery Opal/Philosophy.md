# Fiery Opal
Fiery Opal follows a simplicity-driven design guideline. In order to have excit-
ing emergent features, the base game must be lean and easy to grok. 

## The goal of the game
Your main objective will be to find and wield the Fiery Opal of Power, a mystic
artifact capable of granting godhood to whomever gains possession of it.
This artifact will be hidden in a remote dungeon, and the player will be able
to find it by obtaining clues throughout their run.

## Clues
As the Opal is buried deep underground, where stronger enemies spawn, finding
its temple without outside help is basically impossible. However, the game has a
way of directing the player through clues: pieces of information that may or
may not reveal a detail about the location of the temple. Not all clues are
true, but those that are will be congruent and thus it will be possible to
discern the false clues through a process of elimination. Not all clues will be
related to the Opal, most of them will be about other in-game relics and places.

The player always brings a journal with them, on which they can write notes and
copy pages from other books. Clues should be written here, but it's not enforced

Finally, clues are generally obtained in the following ways:
    - By reading books
	- By talking to people
	- By deciphering text found in ancient ruins
	- By eavesdropping on secret conversations
	- By finding ancient scrolls and maps

## The World
The world of Fiery Opal is set in a medieval timeline in which magic is possible
, but mostly restricted to very dedicated wizards and the artifacts they create.

Settlements are far and few, but each has its own culture and traditions. Dividing
them are great deserts, impassable mountain ranges, unconfined seas and lush
forests. Therein settle the local communities who decided to alienate themselves
from the greed of humans, and fugitives who seek shelter from the law.

The local flora and fauna are inspired by real world specimens, although some
may altered as to give them magical or mythical properties. Most animals will be
tame-able and capable of working with and for humans.

Settlements are procedurally generated according to these criteria:
- Authoritativeness: Determines the social hierarchy.
- Economy: Determines how wealth is distributed (0=left<->right=1).
- Influence: the area over which this settlement expands
- Density: a function that maps the probability of spawning buildings.
- Wealth: higher wealth generates richer buildings.
- State Religion: references the settlement's policies on religion
- Industries: the main ways for this settlement to generate wealth


Example:
- Authoritativeness: 1: Very rigid social hierarchy, people are divided into
  categories, some of which have duties and some of which have privileges.

  Leader: Most important person of the entire settlement. Everyone obeys to him.
  Coordinates matters such as war, taxation and lawmaking.

  Spymaster: Coordinates spies and gathers information for the leader.


- Economy: 0: Initial wealth is distributed equally.
- Influence: 10: 10 world-map tiles radius.
- Density: f(x, y) -> inv_dist(x, y, centerX, centerY); // More buildings at center
- Wealth: .8: Buildings are luxurious and the wealthy have lots of personal items.
- State Religion: Freedom of Religion
- Industry: Beet farming, Carrot farming, Cow produce
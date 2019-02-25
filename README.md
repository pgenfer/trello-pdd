# trello-pdd
A cmd line tool for [puzzle driven development](https://www.yegor256.com/2010/03/04/pdd.html) with [Trello](https://trello.com/).

![A trello board with cards created by trello-pd](/images/trello-pd.png?raw=true "Optional Title")

## The problem

During my work on side projects, I quite often encounter the following difficulties:

 - not knowing where to continue after a longer break
 - some tasks are simply to large to complete them in short, interrupted time periods (as you normally have during your free time)

I read an interesting article about [puzzle driven development](https://www.yegor256.com/2010/03/04/pdd.html). This concept sounded like a promising development approach to solve these issues, so I wanted to give it a try:
While the whole concept is a bit more sophisticated, I would like to concentrate on a more simplified approach I found suitable for my side projects:

During this development workflow, you organize your work in tasks which do not take longer than 30 minutes. After this timeframe is exhausted, there are two possibilities:

 1. The task is finished. Well done, continue with the next one.
 2. The task is not finished and you have some construction sites scattered around your code base.
 
The second case is the one where puzzle driven development comes into play:

Instead of continuing with your task, you simply document the places in your code where additional work needs to be done, and any of these places is the starting point for a new task.
It's important that you define cleary what needs to be done at the specific position in code and what is the expected result (simply write your documentation in a way that any other person could start working on the issue without consulting you any further).
In that way, you can split your original task up into several smaller pieces (of a *puzzle*). 
You still have only 30 minutes to finish any of your new tasks, and in case there is still some work remaining, you continue creating new follow up issues.

In that way the two major issues mentioned at the beginning are solved:

- you always have some well documented starting points for your work. Even after some longer breaks you should be able to catch up at any of these positions.
- even if a task seems to big, you can just start implementing it and if you're not able to finish it, simply split it up into remaining subtasks.

## Implementation

trello-pdd is a simple command line tool that tries to support the developer with this approach. The idea is to simply add descriptions of new tasks in your code as documentation:

```javascript
/// TODO 52: Adjust the size of the text box for connections.
/// Currently the textbox for connection names is too small and also placed wrongly.
/// It should also be checked if some of the hardcoded widths/heights in the connection
/// can somehow be replaced by variables.
```
When trello-pd parsed your source code and finds a comment of the given form, it does the following:
 1. it connects to your Trello board
 2. It checks if there is a card with the number ```52```
 3. If the card exists, a new card will be created and linked to card ```52``` (the parent card)
 4. The additional text in the comment will be added as description to the trello card.
 
Currently, the following format is supported:

```javascript
/// TODO [parent card number] [label]: [title of card]
/// [task description]
```

In case the parent card number is omitted, trello-pd will create a new root card.

The list were the cards should be added can be set via configuration file.


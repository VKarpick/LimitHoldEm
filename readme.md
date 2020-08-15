# Limit Hold Em

Console game of Limit Hold Em with AI using Smooth UCT.  
Written with C# and requires Newtonsoft.Json.

Original paper for Smooth UCT can be found [here.](https://www.davidsilver.uk/wp-content/uploads/2020/03/smooth_uct-1.pdf)  
The odds calculations are courtesy [Keith Rule.](https://www.codeproject.com/Articles/12279/Fast-Texas-Holdem-Hand-Evaluation-and-Analysis)  
Opening hand odds are courtesy [caniwin.com.](https://caniwin.com/texasholdem/preflop/heads-up.php)  
Monte Carlo Tree Search code is largely a conversion to C# of [this Python implementation.](https://github.com/int8/monte-carlo-tree-search)

When playing, use c to check/call, r to raise, and f to fold.  To quit game, enter q or quit.

## AI

The intention was to recreate the original paper as best as I could understand it.  Differences of note are that pot sizes are measured in small blinds instead of big blinds and expected hand strengths aren't squared - with the discretization, this seemed unnecessary.  

Thanks to the odds calculations from Keith Rule, no hand values are estimated.  However, if you're looking for speed ups, using a Monte Carlo simulation, especially on the flop, should help (and shouldn't cause significant differences in the policy).  

The JSON file for the policy is too large for github so no pretrained policy is provided.  


## Background

Outside of some minor projects years ago, this is my first dive into C#.  The choice of C# was made entirely as a result of it being the language used in Keith Rule's odds calculations.  

It certainly isn't the prettiest of games but that was never the intention.  It can be considered a continuation of my education through self-learning of reinforcement learning.

## Hat Tips

[David Silver's YouTube lectures and slides](https://www.davidsilver.uk/teaching/)  
Reinforcement Learning:  An Introduction - Sutton and Barto

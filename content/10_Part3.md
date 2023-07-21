---
title: Side effects
---

# PART 3 - Getting pragmatic.

The first part of this book focused on the basic ideas and types of Rx. In the second part, I showed the operators Rx offers, enabling us to define the transformations and computations we want to apply to our source data. This second part was essentially functional programming—Rx's operators are mostly like mathematical functions, in that they will invariably behave in the same way for particular inputs. They are unaffected by the state of the world around them, and they also do nothing to change its state. In functional programming, such mechanisms are sometimes described as _pure_.

This _purity_ can help us understand what our code will do. It means we don't need to know about the state of the rest of our program in order to understand how one particular part functions. However, code that is completely detached from the outside world is unlikely to achieve anything useful. In practice, we need to connect these pure computations with more pragmatic concerns. The [Creating Observable Sequences chapter](03_CreatingObservableSequences.md) already showed how to define observable streams, so we've already looked at how to connect real world inputs into the world of Rx. But what about the other end? How do we do something useful with the results of our processing?

In some cases, it might be enough to do work inside `IObserver` implementations, or using the callback-based subscription mechanisms you've already seen. However, some situations will demand something more sophisticated. So in this third part of the book, we will look at some of the features Rx offers to help connect processes of the kind we looked at in part 2 with the rest of the world.


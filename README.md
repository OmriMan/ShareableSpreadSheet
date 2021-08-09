# ShareableSpreadSheet
A solution to the reader-writer problem effectively, an implementation that allows multiple reading and multiple writing simultaneously (as much as possible).
In the Introduction to Operating Systems course we were asked to implement an in-memory shared spreadsheet management object. We encountered a problem very similar to the known readers-writer problem. The difference between our problem and the classic reader-writer problem is that in our case, it is not effective to lock the entire sheet (source) for each writer. So our problem is an extension of the reader-writer problem, we can call this problem the "Regular User - Absolute Writer" problem. When a regular user includes all readers and a large part of the writers, for example a writer who wants to update an cell(setCell) (there is no reason to lock the whole sheet because of a specific cell that can be very small part of the sheet) and an absolute user includes the users who change the table drastically (for example: exchange / add columns/rows , Save and load). Later we encountered new challenge - the setConcurrentSearchLimit function which limits the number of searchers (4 search functions) and our solution to this challenge is to add another user to our program (We will continue with the line of calling each problem by a new and bombastic name) and lets upgrade the name to the "Regular User – Search Regular User -Absolute Writer" problem.
All users operate in the same format:
<Entry section> - 3 different section (for each user-Regular/Searcher/Absolute writer)
<Critical section>  - unique and depending on the operation – the operation itself
<Exit section> - - 3 different section (for each user-Regular/Searcher/Absolute writer)
We used 4 locks (semaphore(binary semaphore)):
1. resource – lock the sheet
2. Queue – maintains an organized entry for all users
3. non_absolute_counter_save – lock the access to the non absolute counter
4. searcher_counter_save – lock the access to the searcher counter.
We use 2 mutex arrays:
1.	Mutex[] rows_lock – mutex for every row.
2.	Mutex[] cols_lock – mutex for every column.
For regular users, we have 2 arrays of locks(mutex), If a regular user access a cell, we lock the row and column for that cell using these arrays. this means that many writers(non-absolute)  can access the sheet even when there are other users that have access at the same time.


We used 3 atomic variables:
1.	non absolute counter – count the non absolute user that have an access to our resource.
2.	Searcher counter - count the searcher users that have an access to our resource.
3.	ConcurrentSearchLimit – the limit of the searcher users (if its -1 – no limit)

Once there is at least one regular user - the resource is locked from an absolute writer,
If non absolute counter>=1 the access to the sheet is locked to absolute writer,
If non absolute counter=0 the access to the sheet is unlock, every user that need access can get it.
If there is one absolute writer, the access to the sheet is locked for all users.
How did we implement it?
First, Queue lock maintains an organized entrance and make sure that we handle each user  entry in a "personal" way, we will not miss anyone and it will be in a controlled and orderly manner.
The counter locks(non_absolute_counter_save and searcher_counter_save) maintain access to counters so we sure that once a user enter or exit, only "he" can access and update the counter. 
The resource locks the table from users, if there are now regular users then access to the sheet is locked to absolute writers and if there is an absolute writer the access is locked to all users.

We divided the users into 3 types and for each type we wrote different entry and exit sections:
Regular:
Entry: 
wait for the Queue lock to allow him to enter
wait for the regular user counter to access,checks whether he is the first regular user - if so, locks the resource lock and updating the regular counter.
releasing the Queue and the Regular locks.
Exit:
wait for the regular user lock to access the regular counter,checks whether he is the last regular user - if so, release the resource lock and updating the regular counter.
releasing the Regular lock.
Searcher :
Entry :
Checks if there is a limit on the number of searchers -If not(ConcurrentSearchLimit =-1), behave like a Regular user.
If there is a limit –
wait for the Queue lock to allow him to enter
wait for the searcher lock to access the searcher counter, checks if there is place to another searcher(if searcher_counter== ConcurrentSearchLimit return false and release locks) ,else (searcher_counter!= ConcurrentSearchLimit) update the searcher counter. 
wait for the regular user lock to access the Regular counter,checks whether he is the first regular user - if so, locks the resource lock and updating the regular counter.
releasing the Queue,searcher and Regular locks.
Exit:
Checks if there is a limit on the number of searchers -If not(ConcurrentSearchLimit =-1), behave like a Regular user.
If there is a limit –
wait for the regular user lock to access the regular counter,checks whether he is the last regular user - if so, release the resource lock and updating the regular counter.
releasing the Regular lock.
wait for the searcher lock to access the searcher counter and update the searcher counter. 

Absolute writer:
Entry:
wait for the Queue lock to allow him to enter
wait for the resource lock to access the sheet
release Queue lock
Exit:
release resource lock


Our implementation solves the problem in that many users can access the table at the same time, most writers (non-absolute) will not have to wait long until they get access - they will wait just like readers. An absolute writer will perform his critical section when only he has access to the source (sheet).  
 
![image](https://user-images.githubusercontent.com/81520237/128755090-5de0bb2a-413c-4bde-acdc-1341cca8a6bf.png)
![image](https://user-images.githubusercontent.com/81520237/128754745-eca6810b-cc68-458c-866e-cfc5e4a85b68.png)
 ![image](https://user-images.githubusercontent.com/81520237/128754887-5a858414-effb-4118-9322-8b2e81ca2903.png)
![image](https://user-images.githubusercontent.com/81520237/128754989-662939e6-0254-496e-93c7-4f0207b915bc.png)


  

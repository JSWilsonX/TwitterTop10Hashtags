# TwitterTop10Hashtags
The TwitterTop10Hashtags is a console application that utilizes _Twitter API v2 sampled stream_ endpoint and processes incoming tweets to compute various statistics.

## Twitter API Token
This application requires a Bearer Token to be in the environment variable **TwitterBearerToken**. The **TwitterBearerToken** will look something like _AAAAA...dcJXhs_. I chose this option as I don't know enough about where you would be running the application. 

## Statistics
The application generates the following statistics each minute.
* Total number of tweets received
* Total number of tweets received this minute
* Total number of hashtags received
* Top 10 hashtags

A sample of the output is included in the file **Sample_Output.txt**. It was obtained by running the application for an hour and then stopping it. I received between 6 and 7,166 tweets per minute. CPU utilization remained below 7%. Network traffic was consistently low.

## Notes
This was a fun and interesting exercise. I had not used the Twitter API before, so I learnt something new. If I had more time I would have found and used an available library for accessing the API.


# MultiSpeakBrokerLoadTest
This application is for testing high availability using HAProxy and the MultiSpeakBroker web application.
The MultiSpeakBroker web application brokers web services from one vendor to another vendor.  

The components making up this stack are the following:

AMP request : The Command Brokering is to be available 100% of the time.

## TODOS
* Add https to Haproxy
* ASP.net core -> run on linux

# Hardware (2016-10-31)
Cores and CPU is dependant on what is available when purchasing the hardware.

* amp-ha1 (10.86.1.91) Windows Server 2012, with IIS, running ASP.net and .Net 4.0 and above, 1 instance mongodb
    * Dual E5-2643 v3 Processors (6 cores @3.4–3.7GHz each)
    * 64 GB of RAM - * MB RAM calculate size of mongdb for 400,000 records. RAM needed for Web App and Server?
    * 2x Intel 320 300GB SATA SSDs (RAID 1) (See the mongodb section on calculation fo the space)
    * Dual 10 Gbps network (Intel X540/I350 NDC)
* amp-ha2 (10.86.1.92) Windows Server 2012, with IIS, running ASP.net and .Net 4.0 and above, 1 instance of mongodb and arbiter
    * Dual E5-2643 v3 Processors (6 cores @3.4–3.7GHz each)
    * 64 GB of RAM - * MB RAM calculate size of mongdb for 400,000 records.
    * 2x Intel 320 300GB SATA SSDs (RAID 1) (See the mongodb section on calculation fo the space)
    * Dual 10 Gbps network (Intel X540/I350 NDC)
* ??????? (10.87.1.95) CentOS, port 80 open, with HAProxy installed.
    * Dual E5-2650 Processors (8 cores @2.0–2.8GHz each)
    * 64 GB of RAM (4x 16 GB DIMMs)
    * 2x Seagate Constellation 7200RPM 1TB SATA HDDs (RAID 10) (Logs)
    * Dual 10 Gbps network (Intel X540/I350 NDC) - Internal (DMZ) Traffic
    * Dual 10 Gbps network (Intel X540) - External Traffic
    * http://cbonte.github.io/haproxy-dconv/1.7/intro.html#3.5

The amp-ha1 and amp-ha2 need to reserve room for normal operations in memory and show only consume
75% of the RAM, which is 48GB of data in memory for these servers.  To be safe we can just cap the
collection at 30GB.

# High Availability Setups SQL Server and MongoDB

## SQL Server 
* Pros
    * Saves you when hardware fails
    * Simplifies maintenance and patching

* Cons
    * Storage is a single point of failure
    * Tricky to setup and pair with other technologies.
    * (Hypervisors… virtualization)
    * SQL Server – Loss of data on a reboot.  Any data being sent to SQL Server or being imported on the server is lost and has to recover.
    * SQL Server unavailable on reboot.

SQL Server storage is still single point of failure now.  On a windows box, which has to be rebooted.
Single copy of data… lose storage you lose it all.

## Failover Clustering - Requires OS Feature in Windows
Supported in Standard and Datacenter editions of Windows Server 2012 and R2.  Only a 2 node cluster though.  
Idle node of sql does not require lic. Connection Strings Change since we connect with Network Name and Instance 
Name… with port number.  Network Name goes with Virtual Name, instance name is like … instance is independent 
of physical hardware.

There is a time when a node is shut down and the data has to transition to the other sql instance where 
no data will get through.  The application will have to retry the request. 
(This will not work for incoming requests which need to write to the database…. We will have to recode 
retries into the application layer.)

How long does failover take… need transaction log back ups… instead of simple.

## Always On Availability Group…
* Requires Enterprise Edition, and priced per core, and the secondary server has to remain idle with no workload.
* Data can stream between each sql server instance

# High Availability Setups for Servers

## Windows NLB (Network Load Balancer)
Installing NLB (Brian Watson set this up on amp-ha1 and amp-ha2)

Floating IP -> 10.87.1.90
The floating IP ponints to both amp-ha1 and amp-ha2.  NLB only points to one server.  The other server
remains offline or under utlized.   The floating IP will remain connected to amp-ha1, until the 
server goes offline or the NLB setting is set to be removed from the 

Here is the NLB http://10.87.1.90/.

NLB always hits one server, HAProxy is setup to round robin.  The load is distributed between two servers.

Tests
Stop the website on amp.ha1
NLB http://10.87.1.90/ still hits amp.ha1, and website is down and our clients (amp members) will be
unable to send requests.

haproxy hits amp.ha2, and works.

Stop the website on amp.ha2
NLB http://10.87.1.90/ still hits amp.ha1, and website is up.
haproxy hits amp.ha1, and website is up.

Restart Server amp.ha1
NLB takes longer to figure out the server is missing from the cluster before switching the the available server.
haproxy found the online server fast. 

## Test Remove amp-ha1 from in the cluster.
* Here is the NLB http://10.87.1.90/ then points to HA2, as it should.
* Restart the HA1, and this took 8-10 minutes to come back online.
* Very slow... 

# HAProxy Setup CentOS7

Notes:
Testing Results
Here is the url for haproxy. Each time you hit the side it will pick the other server.
http://10.87.1.95/

Tests
Stop the website on amp.ha1
Haproxy hits amp.ha2, and works.

## Overview of Results  HAProxy vs NLB

## NLB Benefits
1. One Benefit to the NLB is the same IP will be used for all servers and with HAProxy we would 
need another HAProxy server and a floating IP setup if we reboot the HAproxy server. This is fairly
typical setup in the industry.

## NLB Cons
1. Slow recover time.  8-10 minuts for a server to rejoin the cluster.
2. NLB only works if the server is offline. If the web site has issues or is taken offline incoming commands
would not be sent to working server.

## HAProxy Benefits
1. HAProxy works for all reasons the site or server is down.
2. HAProxy distributes the server work load.
3. HAProxy runs site checks on each server making sure the website is responding.
4. HAProxy max connections
5. Timeouts, we can set the period for this to occur
6. Retries, if HAProxy timesout, we can set it to retry the http connection.

In this case HAProxy can provide
a tremendous help by enforcing the per-server connection limits to a safe value
and will significantly speed up the server and preserve its resources that will
be better used by the application.

## HAProxy Cons
1. Requires linux
2. Requires second server which waits for the floating IP to be assigned if the active HAProxy server goes down.

# Installation Issues
* Had to open port 80 on CentOS
* Windows Servers did not have ASP.net installed.

# CentOS7

## How to SSH using windows

## How to SSH using POSIX

Failover Options – Single Site Single Subnet

http://geoserver.geo-solutions.it/edu/en/clustering/load_balancing/haproxy.html

OPEN PORT 80
http://ask.xmodulo.com/open-port-firewall-centos-rhel.html

```bash
    $ sudo firewall-cmd --zone=public --add-port=80/tcp --permanent
    $ sudo firewall-cmd --reload

    --More examples
    $ sudo firewall-cmd --permanent --zone=public --add-service=http
    $ sudo firewall-cmd --permanent --zone=public --add-port=8181/tcp
    $ sudo firewall-cmd --reload
```
Test if Port 80 is open using Nmap from Posix machine

```bash
    $ nmap -Pn 10.87.1.95
    Starting Nmap 6.47 ( http://nmap.org ) at 2016-10-26 23:25 CDT
    Nmap scan report /for 10.87.1.95
    Host is up (0.083s latency).
    Not shown: 998 filtered ports
    PORT   STATE SERVICE
    22/tcp open  ssh
    80/tcp open  http
```

Install HAProxy
```bash 
    $ sudo yum install haproxy
    $ sudo haproxy -v
```
Install Curl for testing the sites on the amp-ha1 and amp-ha2
```bash
    $ sudo yum install curl
```

Setup the HAProxy configuration, 'esc -> :w' saves the file 'esc -> :q' exits the editor.
```bash
    $ sudo vi /etc/haproxy/haproxy.cfg
```

## config examples ##

```bash
    #---------------------------------------------------------------------
    # Example configuration for a possible web application.  See the
    # full configuration options online.
    #
    #   http://haproxy.1wt.eu/download/1.4/doc/configuration.txt
    #
    #---------------------------------------------------------------------

    #---------------------------------------------------------------------
    # Global settings
    #---------------------------------------------------------------------
    global
        # to have these messages end up in /var/log/haproxy.log you will
        # need to:
        #
        # 1) configure syslog to accept network log events.  This is done
        #    by adding the '-r' option to the SYSLOGD_OPTIONS in
        #    /etc/sysconfig/syslog
        #
        # 2) configure local2 events to go to the /var/log/haproxy.log
        #   file. A line like the following can be added to
        #   /etc/sysconfig/syslog
        #
        #    local2.*                       /var/log/haproxy.log
        #
        log         127.0.0.1 local2

        chroot      /var/lib/haproxy
        pidfile     /var/run/haproxy.pid
        maxconn     3000
        user        haproxy
        group       haproxy
        daemon

        # turn on stats unix socket
        stats socket /var/lib/haproxy/stats

    #---------------------------------------------------------------------
    # common defaults that all the 'listen' and 'backend' sections will
    # use if not designated in their block
    #---------------------------------------------------------------------
    defaults
        mode                    http
        log                     global
        option                  httplog
        option                  dontlognull
        option http-server-close
        option forwardfor       except 127.0.0.0/8
        option                  redispatch
        retries                 3
        timeout http-request    10s
        timeout queue           1m
        timeout connect         10s
        timeout client          1m
        timeout server          1m
        timeout http-keep-alive 10s
        timeout check           10s
        maxconn                 3000

    #---------------------------------------------------------------------
    # main frontend which proxys to the backends test site
    #---------------------------------------------------------------------
    frontend http-in
        bind *:80
        stats uri /haproxy?stats
        default_backend backend_servers
        option          forwardfor

    #---------------------------------------------------------------------
    # define backend for test site
    #---------------------------------------------------------------------
    backend backend_servers
        balance     roundrobin
        server  amp-ha1  10.86.1.91:81 check
        server  amp-ha2  10.86.1.92:81 check
```

Check if HAPRoxy is running

```bash
    $ ps aux | grep haproxy
```

If HAProxy is not running do this... 
DO I REALLY NEED THIS, PORT 80 WAS OFF BEFORE I Changed this
```bash
    $ sudo vi /etc/rsyslog.conf
    //Uncomment this line to enable the UDP connection:
    $ModLoad imudp
    $UDPServerRun 514
```

```bash
--start at Boot 
    systemctl enable haproxy
    systemctl restart rsyslog
    systemctl start haproxy
    systemctl restart haproxy
```

For logging it is highly recommended to have a properly configured syslog daemon
and log rotations in place.

State change is notified in the logs and stats page with the failure reason
    (eg: the HTTP response received at the moment the failure was detected). An
    e-mail can also be sent to a configurable address upon such a change ;

all algorithms above support per-server weights so that it is possible to
    accommodate from different server generations in a farm, or direct a small
    fraction of the traffic to specific servers (debug mode, running the next
    version of the software, etc);

Note, another benefit is we can setup a port for each Member, or frontend and backend servers
and create seperate logs... and move our app servers around and not require the customer to ever
change their urls in out applications.

Advanced Features:
http://cbonte.github.io/haproxy-dconv/1.7/intro.html#3.4

required tools for debugging:
socat
halog
tcpdump
strace

- socat (in order to connect to the CLI, though certain forks of netcat can
    also do it to some extents);

  - halog from the latest HAProxy version : this is the log analysis tool, it
    parses native TCP and HTTP logs extremely fast (1 to 2 GB per second) and
    extracts useful information and statistics such as requests per URL, per
    source address, URLs sorted by response time or error rate, termination
    codes etc... It was designed to be deployed on the production servers to
    help troubleshoot live issues so it has to be there ready to be used;

  - tcpdump : this is highly recommended to take the network traces needed to
    troubleshoot an issue that was made visible in the logs. There is a moment
    where application and haproxy's analysis will diverge and the network traces
    are the only way to say who's right and who's wrong. It's also fairly common
    to detect bugs in network stacks and hypervisors thanks to tcpdump;

  - strace : it is tcpdump's companion. It will report what HAProxy really sees
    and will help sort out the issues the operating system is responsible for
    from the ones HAProxy is responsible for. Strace is often requested when a
    bug in HAProxy is suspected;


## MultiSpeakBrokerLoadTesting application
How do I used this application.
1. Create some meters using this script.  
2. I wonder if we use MeterAddNotification we could get this setup to work... one Method to insert
the meters and all meta data along with the Customer, Account, Location, etc... ALMA/ALMH

# mongodb
> db.version()  
3.2.8

> db.system.indexes.find()
{ "v" : 1, "key" : { "_id" : 1 }, "name" : "_id_", "ns" : "MultiSpeakBroker.User" }
{ "v" : 1, "key" : { "_id" : 1 }, "name" : "_id_", "ns" : "MultiSpeakBroker.Vendor" }
{ "v" : 1, "key" : { "_id" : 1 }, "name" : "_id_", "ns" : "MultiSpeakBroker.CompanyReadSource" }
{ "v" : 1, "key" : { "_id" : 1 }, "name" : "_id_", "ns" : "MultiSpeakBroker.Subscriber" }
{ "v" : 1, "key" : { "_id" : 1 }, "name" : "_id_", "ns" : "MultiSpeakBroker.BrokeredRequest" }
> 

## Capped Collections
db.BrokeredRequest will need to be a capped collection.  We can 


https://docs.mongodb.com/manual/core/capped-collections/

You must create capped collections explicitly using the db.createCollection() method, which is a helper in the mongo shell for the create command. When creating a capped collection you must specify the maximum size of the collection in bytes, which MongoDB will pre-allocate for the collection. The size of the capped collection includes a small amount of space for internal overhead.

```mongo
db.createCollection("log", \{ capped : true, size : 5242880, max : 5000 \} )
```

db.getProfilingLevel()
db.setProfilingLevel(2)
db.system.profile.find()

> db.system.profile.find().sort({millis: -1}).limit(1).pretty()


clean up
db.getProfilingLevel()
db.system.profile.remove()



> use MultiSpeakBroker
switched to db MultiSpeakBroker
> db.stats()
{
	"db" : "MultiSpeakBroker",
	"collections" : 7,
	"objects" : 133,
	"avgObjSize" : 2462.6766917293235,
	"dataSize" : 327536,
	"storageSize" : 413696,
	"numExtents" : 10,
	"indexes" : 5,
	"indexSize" : 40880,
	"fileSize" : 67108864,
	"nsSizeMB" : 16,
	"extentFreeList" : {
		"num" : 0,
		"totalSize" : 0
	},
	"dataFileVersion" : {
		"major" : 4,
		"minor" : 22
	},
	"ok" : 1
}

Bad move the collection is not capped and is required to be capped.
db.collection.isCapped()

> db.BrokeredRequest.stats();
{
	"ns" : "MultiSpeakBroker.BrokeredRequest",
	"count" : 85,
	"size" : 314032,
	"avgObjSize" : 3694,
	"numExtents" : 3,
	"storageSize" : 335872,
	"lastExtentSize" : 262144,
	"paddingFactor" : 1,
	"paddingFactorNote" : "paddingFactor is unused and unmaintained in 3.0. It remains hard coded to 1.0 for compatibility only.",
	"userFlags" : 1,
	"capped" : false,
	"nindexes" : 1,
	"totalIndexSize" : 8176,
	"indexSizes" : {
		"_id_" : 8176
	},
	"ok" : 1
}
> 

### Convert a collection to capped
If we have forgotten to capp the collection this is how you can do perform this funcation 
if the collection was not created as a capped collection when the code issues the first
request to add a record to BrokeredRequest collection.

db.runCommand({"convertToCapped": "mycoll", size: 100000});

The size argument is always required, even when you specify max number of documents. 
MongoDB will remove older documents if a collection reaches the maximum size limit before it reaches the 
maximum document count.

## Copy the database…
One quick way to make a copy of a database to test with and leave the existing database operational 
is to use the db.copyDatabase feature.  
> db.copyDatabase('records', 'archive_records')
db.inventory.remove({})


Review 
> db.BrokeredRequest.find({},{_id:0,ClientRequestDate:1,ClientTransactionID:1}).sort( { ClientRequestDate: 1 } )
## Replica Set Setup

## CentOS7 Logs for debugging

# IIS Tip & Treat
Added customer Response Header so I can parse this out when load testing… to both amp-ha1 and amp-ha2.

Did this in IIS8
https://www.iis.net/configreference/system.webserver/httpprotocol/customheaders

ServerName: amp-ha1

```xml
<configuration>
    <system.webServer>
        <httpProtocol>
            <customHeaders>
                <add name="ServerName" value="amp-ha1" />
            </customHeaders>
        </httpProtocol>
    </system.webServer>
</configuration>
```

You can now see the 'ServerName: amp-ha1' in the tag below.  This might help us in the future
if we want to add more headers to all of our web requests.
```bash
$ curl  -v 10.86.1.91/

> GET / HTTP/1.1
> Host: 10.86.1.91
> User-Agent: curl/7.49.1
> Accept: */*
> 
< HTTP/1.1 200 OK
< Content-Type: text/html
< Last-Modified: Thu, 27 Oct 2016 19:09:54 GMT
< Accept-Ranges: bytes
< ETag: "84a9a5b38530d21:0"
< Server: Microsoft-IIS/8.5
< X-Powered-By: ASP.NET
< ServerName: amp-ha1
< Date: Fri, 28 Oct 2016 20:58:43 GMT
< Content-Length: 604
```
# Load Testing using terminal on Mac / Linux
You can issue multiple curl commands in the mac terminal with the following snippet. This will make
1000 calls to the web server and write out the total response time to a file.
```bash
for i in {1..1000}; do curl -s -w "%{time_total}\n" -o /dev/null http://10.87.1.95/MultiSpeak/30ac/1/MDM_Server.asmx >> ha2.txt; done
```

# CentOS7 Logs
To review the logs on CentOS7
```bash
$ cat /var/log/messages
```

The log output below is a sample from HAProxy where we can see each request is round robin'd to each
of the web servers amp-ha1  and then amp-ha2
```log
Oct 28 16:01:28 amp-linux haproxy[10542]: Proxy http-in started.
Oct 28 16:01:28 amp-linux haproxy[10542]: Proxy backend_servers started.
Oct 28 16:02:25 amp-linux haproxy[10543]: 192.168.97.149:61179 [28/Oct/2016:16:02:23.723] http-in backend_servers/amp-ha1 0/0/12/1468/1509 200 23821 - - ---- 2/2/0/0/0 0/0 "GET /MultiSpeak/30ac/1/MDM_Server.asmx HTTP/1.1"
Oct 28 16:02:38 amp-linux haproxy[10543]: 192.168.97.149:61188 [28/Oct/2016:16:02:37.144] http-in backend_servers/amp-ha2 0/0/8/1506/1540 200 23821 - - ---- 2/2/0/0/0 0/0 "GET /MultiSpeak/30ac/1/MDM_Server.asmx HTTP/1.1"
Oct 28 16:04:24 amp-linux haproxy[10543]: 192.168.97.149:61304 [28/Oct/2016:16:04:24.839] http-in backend_servers/amp-ha1 8/0/16/49/105 200 23821 - - ---- 2/2/0/0/0 0/0 "GET /MultiSpeak/30ac/1/MDM_Server.asmx HTTP/1.1"
Oct 28 16:06:15 amp-linux haproxy[10543]: 192.168.97.149:61379 [28/Oct/2016:16:06:14.821] http-in backend_servers/amp-ha2 0/0/11/55/428 200 23821 - - ---- 2/2/0/0/0 0/0 "GET /MultiSpeak/30ac/1/MDM_Server.asmx HTTP/1.1"
Oct 28 16:06:17 amp-linux haproxy[10543]: 192.168.97.149:61379 [28/Oct/2016:16:06:15.248] http-in backend_servers/amp-ha1 2097/0/9/95/2222 200 23821 - - ---- 2/2/0/0/0 0/0 "GET /MultiSpeak/30ac/1/MDM_Server.asmx HTTP/1.1"
Oct 28 16:06:46 amp-linux haproxy[10543]: 192.168.97.149:61406 [28/Oct/2016:16:06:46.226] http-in backend_servers/amp-ha2 3/0/12/119/134 404 2162 - - ---- 2/2/0/0/0 0/0 "GET /MultiSpeak/416/MDM_Server.asmx HTTP/1.1"
Oct 28 16:06:58 amp-linux haproxy[10543]: 192.168.97.149:61420 [28/Oct/2016:16:06:57.218] http-in backend_servers/amp-ha1 0/0/14/1596/1636 200 37487 - - ---- 2/2/0/0/0 0/0 "GET /MultiSpeak/416/1/MDM_Server.asmx HTTP/1.1"
Oct 28 16:07:07 amp-linux haproxy[10543]: 192.168.97.149:61420 [28/Oct/2016:16:06:58.854] http-in backend_servers/amp-ha2 5920/0/11/2927/8884 200 37487 - - ---- 1/1/0/0/0 0/0 "GET /MultiSpeak/416/1/MDM_Server.asmx HTTP/1.1"
```

# References
* http://www.slideshare.net/haproxytech/haproxy-best-practice
* HA High Availability - https://www.digitalocean.com/community/tutorials/an-introduction-to-haproxy-and-load-balancing-concepts
* SQL High Availability - https://www.brentozar.com/archive/2011/12/sql-server-high-availability-disaster-recovery-basics-webcast/
* Windows NLB - https://www.youtube.com/watch?v=y8vUT6F6IVI
* SQL Server Cluster - https://www.brentozar.com/archive/2012/02/introduction-sql-server-clusters/
* SQL Server Failover cluster - https://www.brentozar.com/sql/sql-server-failover-cluster/
* SQL 2 node Failover cluster - http://windowsitpro.com/windows-server-2012/windows-server-2012-building-two-node-failover-cluster
* https://www.digitalocean.com/community/tutorials/how-to-set-up-highly-available-haproxy-servers-with-keepalived-and-floating-ips-on-ubuntu-14-04
* https://www.server-world.info/en/note?os=CentOS_7&p=haproxy
* https://www.howtoforge.com/tutorial/how-to-setup-haproxy-as-load-balancer-for-nginx-on-centos-7/
* https://www.server-world.info/en/note?os=CentOS_7&p=haproxy
* http://tecadmin.net/install-and-configure-haproxy-on-centos/#
* https://www.howtoforge.com/tutorial/how-to-setup-haproxy-as-load-balancer-for-nginx-on-centos-7/
* https://www.iis.net/learn/get-started/whats-new-in-iis-8/iis-80-using-aspnet-35-and-aspnet-45
* https://blogs.msdn.microsoft.com/vijaysk/2012/10/11/iis-8-whats-new-website-settings/
* https://www.upcloud.com/support/haproxy-load-balancer-centos/
* https://www.iis.net/configreference/system.webserver/httpprotocol/customheaders
use std::sync::Mutex;
use std::thread;
use std::time::Duration;
use anyhow::{ bail, Result};

use chrono::prelude::*;
use thiserror::Error;

pub struct Gen(Mutex<Info>);

struct Info {
    instance_id: i64,

    start_timestamp: i64,
    last_timestamp: i64,

    // 回拨次数
    cb: i64,
    // 序列号
    seq: i64,
}

#[derive(Error, Debug)]
enum InitError {
    #[error("The start_year({start_year}) cannot be set to a future time")]
    FutureStartYear {
        start_year: u16
    },
    #[error("The start_year({start_year}) cannot older than 34 years")]
    MaxStartYear {
        start_year: u16
    },
}

#[derive(Error, Debug)]
enum NextError {
    #[error("Max clock back reached({count})")]
    MaxClockBack {
        count: u16
    },
    #[error("Invalid system time({utc_now_timestamp})")]
    InvalidSystemTime {
        utc_now_timestamp: u16
    },
}

impl Gen {
    pub fn new(instance_id: u8, start_year: u16) -> Result<Gen> {
        {
            let utc_now = Utc::now();
            if start_year as i32 > utc_now.year() {
                return bail!("The start_year({start_year}) cannot be set to a future time");
            }
            if utc_now.year() - (start_year as i32) >= 34 {
                return bail!("The start_year({start_year}) cannot older than 34 years");
            }
        }
        let info = Info {
            instance_id: instance_id as i64,

            start_timestamp: NaiveDateTime::parse_from_str(
                format!("{start_year}-01-01 01:00:00 +00:00").as_str(),
                "%Y-%m-%d %H:%M:%S %z",
            )
                .unwrap()
                .timestamp_millis() as i64,

            last_timestamp: 0,
            cb: 0,
            seq: 0,
        };

        Ok(Gen(Mutex::new(info)))
    }

    pub fn next(&mut self) -> Result<i64> {
        let mut info = self.0.lock().unwrap(); //超出函数生命期后被释放

        let utc_now_timestamp = Utc::now().timestamp_millis();

        //当前时间早于起始时间
        if utc_now_timestamp < info.start_timestamp {
            return bail!("Invalid system time({utc_now_timestamp})");
        }

        let mut curr_timestamp =
            utc_now_timestamp - info.start_timestamp;

        if curr_timestamp > info.last_timestamp {
            info.seq = 0;
        } else if curr_timestamp == info.last_timestamp {
            info.seq += 1;
            if info.seq > 4095
            //一毫秒内的请求超过4096次
            {
                thread::sleep(Duration::from_millis(1)); //阻塞一毫秒
                curr_timestamp += 1;
                info.seq = 0;
            }
        } else {
            info.cb += 1;

            //超出了最大回拨次数
            if info.cb >= 8 {
                return bail!("Max clock back reached({})", info.cb);
            }
        }

        info.last_timestamp = curr_timestamp;

        let flake = (info.cb << 60) |
            (curr_timestamp << 20) |
            (info.instance_id << 12) |
            info.seq;

        Ok(flake)
    }
}

impl Iterator for Gen {
    type Item = i64;

    fn next(&mut self) -> Option<Self::Item> { self.next().ok() }
}

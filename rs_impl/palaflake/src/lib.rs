extern crate chrono;

use std::sync::Mutex;
use std::thread;
use std::time::Duration;
use chrono::prelude::*;

pub struct Generator(Mutex<Info>);

struct Info {
    instance_id: i64,

    start_timestamp: i64,
    last_timestamp: i64,

    //回拨次数
    cb: i64,
    //序列号
    seq: i64,
}

impl Generator {
    pub fn new(instance_id: u8, start_year: u16) -> Generator {
        assert!(Utc::now().year() as u16 >= start_year,
                "{}", format!("The start_year({start_year}) cannot be set to a future time"));
        assert!(Utc::now().year() as u16 - start_year < 34,
                "{}", format!("The start_year({start_year}) cannot older than 34 years"));

        Generator(Mutex::new(Info {
            instance_id: instance_id as i64,

            start_timestamp:
            NaiveDateTime::parse_from_str(&format!("{start_year}-01-01 01:00:00 +00:00"), "%Y-%m-%d %H:%M:%S %z")
                .unwrap()
                .timestamp_millis() as i64,

            last_timestamp: 0,
            cb: 0,
            seq: 0,
        }))
    }

    pub fn next(&mut self) -> i64 {
        let mut guard = self.0.lock().unwrap();//超出函数生命期后被释放

        let utc_now_timestamp = Utc::now().timestamp_millis();

        //当前时间早于起始时间
        assert!(utc_now_timestamp >= guard.start_timestamp,
                "{}", format!("Illegal system time({utc_now_timestamp})"));

        let mut curr_timestamp = utc_now_timestamp - guard.start_timestamp;

        if curr_timestamp > guard.last_timestamp {
            guard.seq = 0;
        } else if curr_timestamp == guard.last_timestamp {
            guard.seq += 1;
            if guard.seq > 4095 //一毫秒内的请求超过4096次
            {
                thread::sleep(Duration::from_millis(1));//阻塞一毫秒
                curr_timestamp += 1;
                guard.seq = 0;
            }
        } else {
            guard.cb += 1;

            //超出了最大回拨次数
            assert!(guard.cb < 8,
                    "{}", format!("Out of max clock adjustments({})", guard.cb));
        }

        guard.last_timestamp = curr_timestamp;

        (guard.cb << 60)
            | (curr_timestamp << 20)
            | (guard.instance_id << 12)
            | guard.seq
    }
}

impl Iterator for Generator {
    type Item = i64;

    fn next(&mut self) -> Option<Self::Item> {
        Some(self.next())
    }
}


extern crate chrono;

use std::sync::Mutex;
use std::thread;
use std::time::Duration;
use chrono::prelude::*;

pub struct Generator(Mutex<Info>);

struct Info {
    machine_id: u8,

    start_timestamp: u64,
    last_timestamp: u64,

    cb: u8,
    seq: u16,
}

impl Generator {
    pub fn new(machine_id: u8, start_year: u16) -> Generator {
        assert!(Utc::now().year() as u16 >= start_year, "The start_year cannot be set to a future time");
        assert!(Utc::now().year() as u16 - start_year < 34, "The start_year cannot older than 34 years");

        Generator(Mutex::new(Info {
            machine_id: machine_id.into(),

            start_timestamp:
            NaiveDateTime::parse_from_str(&format!("{}-01-01 01:00:00", start_year), "%Y-%m-%d %H:%M:%S")
                .unwrap()
                .timestamp_millis() as u64,

            last_timestamp: 0,
            cb: 0,
            seq: 0,
        }))
    }

    pub fn next(&mut self) -> u64 {
        let mut guard = self.0.lock().unwrap();//超出函数生命期后被释放

        let utc_timestamp = Utc::now().timestamp_millis() as u64;

        //当前时间早于起始时间
        assert!(utc_timestamp > guard.start_timestamp, "Abnormal system time");
        let mut curr_timestamp = utc_timestamp - guard.start_timestamp;

        if curr_timestamp > guard.last_timestamp {
            guard.seq = 0;
        } else if curr_timestamp == guard.last_timestamp {
            guard.seq += 1;
            if guard.seq == 4096 //一毫秒内的请求超过4096次
            {
                thread::sleep(Duration::from_millis(1));//阻塞一毫秒
                curr_timestamp += 1;
                guard.seq = 0;
            }
        } else {
            guard.cb += 1;
            assert!(guard.cb < 4, "Too many clock adjustments");//超出了最大回拨次数
        }

        guard.last_timestamp = curr_timestamp;

        (guard.machine_id as u64) << 56 | (guard.cb as u64) << 52 | curr_timestamp << 12 | guard.seq as u64
    }
}

impl Iterator for Generator {
    type Item = u64;

    fn next(&mut self) -> Option<Self::Item> {
        Some(self.next())
    }
}


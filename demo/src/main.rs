use std::sync::Mutex;
use std::thread;
use std::time::Duration;

extern crate palaflake;

use palaflake::Gen;

fn main() {
    let mut g = Gen::new(1, 2023).unwrap();

    /*for id in g {
        println!("{} : {}", id, show_binary(id));
    }*/

    let mut before = 0;
    loop {
        let latest = g.next().unwrap();

        assert!(before < latest);
        println!("{} : {}", latest, show_binary(latest));

        before = latest;
        //thread::sleep(Duration::from_millis(233));
    }
}

fn show_binary(num: i64) -> String {
    format!("{:064b}", num)
        .chars()
        .collect::<Vec<char>>()
        .chunks(8)
        .map(|c| c.iter().collect())
        .collect::<Vec<String>>()
        .join(" ")
}
